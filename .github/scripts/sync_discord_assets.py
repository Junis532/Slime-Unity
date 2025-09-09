import os, json, time, datetime, pathlib, requests

DISCORD_TOKEN = os.environ["DISCORD_TOKEN"]
CHANNEL_IDS   = [c.strip() for c in os.environ.get("CHANNEL_IDS","").split(",") if c.strip()]
SAVE_ROOT     = os.environ.get("SAVE_ROOT","assets/discord")
STATE_FILE    = os.environ.get("STATE_FILE",".sync/discord_state.json")
MAX_LOOKBACK  = int(os.environ.get("MAX_LOOKBACK","500"))
RECENT_WINDOW = int(os.environ.get("RECENT_WINDOW","80"))
ALLOW_EXT     = set(e.strip().lower() for e in os.environ.get("ALLOW_EXT","").split(",") if e.strip())
ENABLE_HASH   = os.environ.get("ENABLE_HASH","0") == "1"

POST_CONFIRM   = os.environ.get("POST_CONFIRM","0") == "1"
POST_CHANNELID = os.environ.get("POST_CHANNEL_ID","")
MSG_TEMPLATE   = os.environ.get(
    "POST_MESSAGE_TEMPLATE",
    "✅ {count} file(s) synced.\n{files}\n→ saved under `{save_dir}`"
)

BASE = "https://discord.com/api/v10"
HDRS = {"Authorization": f"Bot {DISCORD_TOKEN}"}

def load_state():
    if not os.path.exists(STATE_FILE): return {}
    try:
        with open(STATE_FILE,"r",encoding="utf-8") as f: return json.load(f)
    except: return {}

def save_state(state):
    os.makedirs(os.path.dirname(STATE_FILE), exist_ok=True)
    with open(STATE_FILE,"w",encoding="utf-8") as f:
        json.dump(state,f,ensure_ascii=False,indent=2)

def list_messages(cid, params):
    r = requests.get(f"{BASE}/channels/{cid}/messages", headers=HDRS, params=params, timeout=30)
    r.raise_for_status()
    return r.json(), r.headers

def post_message(channel_id, content):
    r = requests.post(f"{BASE}/channels/{channel_id}/messages", headers=HDRS,
                      json={"content": content}, timeout=30)
    if r.status_code not in (200,201):
        print("[warn] post_message:", r.status_code, r.text)

def ext_ok(filename):
    return (filename.lower().rsplit(".",1)[-1] in ALLOW_EXT) if ALLOW_EXT else True

def process_messages(cid, msgs, state):
    st = state.setdefault(str(cid), {"last_message_id":"0","seen":{}})
    seen = st["seen"]
    latest_id_seen = int(st["last_message_id"])
    changed_files = False

    for m in msgs:
        mid = int(m["id"])
        atts = m.get("attachments",[]) or []
        if not atts: continue

        base_dir = pathlib.Path(SAVE_ROOT)/str(mid)
        base_dir.mkdir(parents=True, exist_ok=True)

        new_files = []
        for a in atts:
            key = f"{m['id']}#{a['id']}"
            if key in seen: continue
            fn, url = a["filename"], a["url"]
            if not ext_ok(fn): continue
            dst = base_dir/fn
            with requests.get(url,stream=True,timeout=180) as r:
                r.raise_for_status()
                with open(dst,"wb") as f:
                    for chunk in r.iter_content(1024*1024):
                        if chunk: f.write(chunk)
            seen[key] = {}
            new_files.append(fn)

        if new_files:
            changed_files = True
            latest_id_seen = max(latest_id_seen, mid)
            if POST_CONFIRM:
                files_line = "\n".join(f"- {n}" for n in new_files)
                content = MSG_TEMPLATE.format(count=len(new_files), files=files_line, save_dir=str(base_dir))
                target = POST_CHANNELID if POST_CHANNELID else cid
                post_message(target, content)

    if changed_files and latest_id_seen > int(st["last_message_id"]):
        st["last_message_id"] = str(latest_id_seen)
    return changed_files

def main():
    state = load_state()
    repo_changed = False
    for cid in CHANNEL_IDS:
        try:
            st = state.setdefault(str(cid), {"last_message_id":"0","seen":{}})
            last_id = int(st.get("last_message_id","0"))
            collected = []
            after = last_id if last_id>0 else None
            fetched = 0
            while True:
                params = {"limit":100}
                if after: params["after"]=after
                msgs, hdr = list_messages(cid, params=params)
                if not msgs: break
                fetched += len(msgs)
                collected.extend(msgs)
                after = int(msgs[0]["id"])
                time.sleep(0.3)
                if fetched >= MAX_LOOKBACK: break
            collected.sort(key=lambda m:int(m["id"]))
            recent_msgs, _ = list_messages(cid, params={"limit":min(100,RECENT_WINDOW)})
            recent_msgs.sort(key=lambda m:int(m["id"]))
            if process_messages(cid,collected,state) or process_messages(cid,recent_msgs,state):
                repo_changed = True
        except Exception as e:
            print("error channel",cid,":",e)
    if repo_changed:
        save_state(state)
        print("[update] state saved")
    else:
        print("[ok] no new files")

if __name__=="__main__":
    main()

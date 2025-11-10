using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class waterAnim : MonoBehaviour
{
    public List<Sprite> waterSprite;
    public int indexAnim = 0;
    SpriteRenderer usedRenderer;

    private void Start()
    {
        usedRenderer = GetComponent<SpriteRenderer>();
        StartCoroutine(waterFlow());
    }

    IEnumerator waterFlow()
    {
        while (true)
        {
            usedRenderer.sprite = waterSprite[indexAnim];
            yield return new WaitForSeconds(0.2f);
            if (indexAnim + 1 != waterSprite.Count) indexAnim++;
            else if (indexAnim + 1 == waterSprite.Count) indexAnim = 0;
        }

    }
}
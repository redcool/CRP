using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialBlockSetter : MonoBehaviour
{
    static MaterialPropertyBlock block;
    Renderer r;

    public bool updateColor;
    // Start is called before the first frame update
    void Start()
    {
        block = new MaterialPropertyBlock();
        r =  GetComponent<Renderer>();

        block.SetColor("_Color", Random.ColorHSV());
        r.SetPropertyBlock(block);
    }

    // Update is called once per frame
    void Update()
    {
        if (updateColor)
        {
            updateColor = false;
            block.SetColor("_Color",Random.ColorHSV());
            r.SetPropertyBlock(block);
        }
    }
}

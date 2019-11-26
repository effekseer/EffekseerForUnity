using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO; //System.IO.FileInfo, System.IO.StreamReader, System.IO.StreamWriter
using System; //Exception
using System.Text; //Encoding
#if (!UNITY_EDITOR)
using Effekseer;
# endif
public class Test : MonoBehaviour
{
    #if (!UNITY_EDITOR)
    private string[] testList;
    private int frame;
    private EffekseerHandle? handle = null;

    // Start is called before the first frame update
    void Start()
    {
        testList = Resources.Load<TextAsset>("test-list").text.Split(',');

        frame = 0;


    }

    
    void Update()
    {
        int current_effect_number = frame / 120;
        if (current_effect_number >= testList.Length)
        {
            //TODO finish
            return;
        }

        if (frame % 120 == 0)
        {
            if (handle.HasValue)
            {
                handle.Value.Stop();
            }
            string effect_path = testList[current_effect_number].Replace(".efk", "");
            EffekseerEffectAsset effect = Resources.Load<EffekseerEffectAsset>(effect_path);
            handle = EffekseerSystem.PlayEffect(effect, transform.position);

        }
        

        frame++;
        
    }
    #endif
}

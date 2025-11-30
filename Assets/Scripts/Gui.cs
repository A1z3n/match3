using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Gui : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private TextMeshProUGUI text;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetText(string str)
    {
        text.text = str;
    }
}

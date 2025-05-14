using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Debugger : MonoBehaviour
{
    private TMP_Text m_Text;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_Text = GetComponent<TMP_Text>();
        m_Text.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Log(string message)
    {
        m_Text = GetComponent<TMP_Text>();
        Debug.Log(message);
        if (message.Length > 89)
        {
            message = message.Substring(0, 89);
        }
        
        m_Text.text = m_Text.text + "\n" + message ;
    }
}

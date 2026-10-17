using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuManager : MonoBehaviour
{
    private RectTransform options;
    private bool loading;
    private void Start(){AudioListener.volume=PlayerPrefs.GetFloat("SavorPinoy.Volume",1);}
    public void PlayGame()
    {
        if(loading)return;
        if(!Application.CanStreamedLevelBeLoaded("RegionalTriviaHub")){Debug.LogError("RegionalTriviaHub is not enabled in the active Build Profile.");return;}
        loading=true;GameSessionState.ResetBuffs();SceneManager.LoadScene("RegionalTriviaHub");
    }
    public void OpenOptions()
    {
        if(options){options.gameObject.SetActive(true);return;}
        Canvas canvas=FindAnyObjectByType<Canvas>();if(!canvas)return;
        PrototypeUI.Font=TMP_Settings.defaultFontAsset;
        options=PrototypeUI.Panel("OptionsPanel",canvas.transform,0,0,820,520,new Color(.96f,.89f,.72f)).rectTransform;
        PrototypeUI.Text("OptionsTitle",options,"Sound Settings",0,160,700,90,42);
        PrototypeUI.Text("VolumeLabel",options,"Master volume",0,65,700,65,32);
        Slider slider=PrototypeUI.HeatSlider(options,0,-25,600);slider.value=AudioListener.volume;
        slider.onValueChanged.AddListener(value=>{AudioListener.volume=value;PlayerPrefs.SetFloat("SavorPinoy.Volume",value);});
        PrototypeUI.Button("CloseOptions",options,"Back",0,-165,320,80,()=>{PlayerPrefs.Save();options.gameObject.SetActive(false);});
    }
    public void QuitGame(){PlayerPrefs.Save();Application.Quit();}
}

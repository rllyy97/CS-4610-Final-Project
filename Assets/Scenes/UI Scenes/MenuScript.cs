using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    void Start() { }
    void Update() { }

    public void OpenLevel() { SceneManager.LoadScene("Levels"); }
    public void OpenMainMenu() { SceneManager.LoadScene("Menu"); }
    public void OpenAbout() { SceneManager.LoadScene("About"); }
    public void OpenHelp() { SceneManager.LoadScene("Help"); }
    public void OpenPirates() { SceneManager.LoadScene("Pirates"); }
    public void OpenSkaterz() { SceneManager.LoadScene("skaterz"); }
    public void OpenNature() { SceneManager.LoadScene("Nature"); }
    public void OpenSonic() { SceneManager.LoadScene("Sonic"); }
    public void OpenNightmare() { SceneManager.LoadScene("Nightmare"); }
    public void OpenTutorial() { SceneManager.LoadScene("Prototype Scene"); }
    public void OpenHouse() { SceneManager.LoadScene("House"); }
    public void OpenSky() { SceneManager.LoadScene("Sky"); }
}

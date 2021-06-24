using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HMTac;
using SFB;
using TMPro;

public class mainmenu : MonoBehaviour
{
  public GameObject smenu, npmenu, lmenu, lprje;
  public Slider[] sliders;
  public Toggle fs;
  public string filename;
  public DirectoryInfo foldercheck;
  public FileInfo filecheck;
  public TextMeshProUGUI creation;
  public TMP_InputField direcbox, loadbox;
  public DataContainer dc;
  //DDOL is important for passing information between scenes.
  void Start(){dc = GameObject.FindObjectOfType<DataContainer>();
    DontDestroyOnLoad(dc);
    smenu.SetActive(false); dc.p = new prj(); npmenu.SetActive(false); lmenu.SetActive(false);
    foldercheck = new DirectoryInfo(Application.dataPath);
    filecheck = new FileInfo(foldercheck.Parent.FullName + @"\config.json");
    dc.o = new options();
    dc.ddirec = foldercheck.Parent.FullName + @"\projects\";
    dc.direc = dc.ddirec; 
    filename = "";
    updateText();
    if(filecheck.Exists){StartCoroutine(loadConfig());}
    else{dc.o.f = false; Screen.fullScreen = false; dc.o.setVols(); StartCoroutine(writeConfig());}
    //hey unity, where's screen.setres(resolution)
    //you have the structure for it
    //also just using native res produces strange behavior for me (defaults to 800 * 600 ???) so,
    Screen.SetResolution(Screen.currentResolution.width,Screen.currentResolution.height,false,Screen.currentResolution.refreshRate);
  }
  public void editNewProject(){editNewProject((filename == "" ? "unnamed" : filename));}
  public void editNewProject(string name){dc.p.name = name;
    dc.p.names = new string[1];
    dc.p.names[0] = "unnamed";
    dc.direc = dc.direc + name + "\\";
    dc.p.wm = true;
    dc.p.palettelen = 0; dc.p.lastmap = 0; dc.p.mapcount = 0;
    //system is built around longterm storage, to be used in an actual game
    //TODO: I mean, at some point
    foldercheck = Directory.CreateDirectory(dc.direc);
    foldercheck.CreateSubdirectory("paints");
    foldercheck.CreateSubdirectory("previews");
    foldercheck.CreateSubdirectory("maps");
    foldercheck.CreateSubdirectory("sounds");
    foldercheck.CreateSubdirectory("music");
    foldercheck.CreateSubdirectory("character_sheets");
    foldercheck.CreateSubdirectory("models");
    StartCoroutine(load(false));}
  public IEnumerator loadProjectfile(string path){filecheck = new FileInfo(path);
    if(filecheck.Exists){dc.l = true;
    FileStream file = filecheck.Open(FileMode.Open,FileAccess.ReadWrite);
    ValueTask<prj> t = JsonSerializer.DeserializeAsync<prj>(file);
    yield return new WaitUntil(() => t.IsCompleted);
    dc.p = t.Result;
    file.Close();
    dc.direc = path.Substring(0,path.Length - (dc.p.name.Length + 4));}
    else{lprje.SetActive(true);}
    yield break;}
  public void loadProject(){StartCoroutine(editProject());}
  public IEnumerator editProject(){yield return loadProjectfile(filename);
    if(dc.l){yield return load(true);}
    yield break;}
  public void openProjectFile(){string[] bepis = StandaloneFileBrowser.OpenFilePanel("Select Project file...", dc.ddirec, "prj", false);
  try{if(bepis[0] != null){setfilename(bepis[0]);}}
  catch(System.IndexOutOfRangeException){}}
  IEnumerator load(bool l){AsyncOperation a = SceneManager.LoadSceneAsync(1);
    yield return new WaitUntil(() => a.isDone);
    yield break;}
  //both methods exist in case people keep a filewindow open and use copy paste from there
  public void setCD(string d){if(d != ""){dc.direc = d;}}
  public void setCD(){string[] bepis = StandaloneFileBrowser.OpenFolderPanel("Set Project Directory...",dc.ddirec,false);
  try{if(bepis[0] != null){setCD(bepis[0] + "\\");
    direcbox.text = bepis[0] + "\\";}}
  catch(System.IndexOutOfRangeException){}}
  public void setfilename(string s){filename = s;}
  public void setfilename(){string[] bepis = StandaloneFileBrowser.OpenFilePanel("Select Project File...",dc.ddirec,"prj",false);
    try{if(bepis[0] != null){setfilename(bepis[0]);}}
    catch(System.IndexOutOfRangeException){}}
  public void updateText(){creation.text = dc.direc + filename;
    loadbox.text = filename;}
  public void leave(){Application.Quit();}
  public void saveConfig(){StartCoroutine(writeConfig());}
  //config is stored in the same directory as the executable as a json file
  //makes program portable, but delete config.json if you actually do move it around
  public IEnumerator writeConfig(){FileStream file = new FileStream(foldercheck.Parent.FullName + @"\config.json", FileMode.Create, FileAccess.Write);
  Task t = JsonSerializer.SerializeAsync<options>(file,dc.o);
  while(!t.IsCompleted){yield return null;}
  file.Close();
  yield break;}
  public void setMVol(float f){dc.o.mVol = f;}
  public void setFXVol(float f){dc.o.fxVol = f;}
  public void setMUVol(float f){dc.o.muVol = f;}
  public void setAVol(float f){dc.o.aVol = f;}
  public void setFS(bool b){dc.o.f = b; Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, b, Screen.currentResolution.refreshRate);}
  public IEnumerator loadConfig(){FileStream file = new FileStream(foldercheck.Parent.FullName + @"\config.json", FileMode.Open, FileAccess.Read);
    ValueTask<options> t = JsonSerializer.DeserializeAsync<options>(file);
    while(!t.IsCompleted){yield return null;}
    dc.o = t.Result;
    file.Close();
    Screen.fullScreen = dc.o.f;
    fs.isOn = dc.o.f;
    sliders[0].value = dc.o.mVol;
    sliders[1].value = dc.o.muVol;
    sliders[2].value = dc.o.fxVol;
    sliders[3].value = dc.o.aVol;
    yield break;}
}

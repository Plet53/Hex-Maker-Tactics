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

public class mainmenu : MonoBehaviour {
  #region datastore
  public GameObject lprje;
  public Slider[] sliders;
  public Toggle fs;
  public string filename {get; set;}
  public DirectoryInfo foldercheck;
  public FileInfo filecheck;
  public TextMeshProUGUI creation;
  public TMP_InputField direcbox, loadbox;
  public DataContainer dc;
  #endregion
  void Start() {
    //DDOL is important for passing information between scenes.
    dc = GameObject.FindObjectOfType<DataContainer>();
    DontDestroyOnLoad(dc);
    dc.p = new Prj();
    foldercheck = new DirectoryInfo(Application.dataPath);
    filecheck = new FileInfo(foldercheck.Parent.FullName + @"\config.json");
    dc.o = new Options();
    dc.ddirec = foldercheck.Parent.FullName + @"\projects\";
    dc.direc = dc.ddirec; 
    filename = "";
    UpdateText();
    if (filecheck.Exists) StartCoroutine(LoadConfig());
    else {
      dc.o.f = false; 
      Screen.fullScreen = false; 
      dc.o.setVols(); 
      StartCoroutine(WriteConfig());
    }
    //hey unity, where's screen.setres(resolution)
    //you have the structure for it
    //also just using native res produces strange behavior for me (defaults to 800 * 600 ???) so,
    Screen.SetResolution(Screen.currentResolution.width,Screen.currentResolution.height,dc.o.f,Screen.currentResolution.refreshRate);
  }
  #region menu
  public void EditNewProject() {EditNewProject((filename == "" ? "unnamed" : filename));}
  public void EditNewProject(string name) {
    dc.p.name = name;
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
    StartCoroutine(Load(false));
  }
  public void LoadProject() {StartCoroutine(EditProject());}
  public IEnumerator EditProject() {
    yield return LoadProjectfile(filename);
    if(dc.l){yield return Load(true);}
    yield break;
  }
  public IEnumerator LoadProjectfile(string path) {
    filecheck = new FileInfo(path);
    if(filecheck.Exists){dc.l = true;
    FileStream file = filecheck.Open(FileMode.Open,FileAccess.ReadWrite);
    ValueTask<Prj> t = JsonSerializer.DeserializeAsync<Prj>(file);
    yield return new WaitUntil(() => t.IsCompleted);
    dc.p = t.Result;
    file.Close();
    dc.direc = path.Substring(0,path.Length - (dc.p.name.Length + 4));}
    else{lprje.SetActive(true);}
    yield break;
  }
  public void OpenProjectFile() {
    string[] TSA = StandaloneFileBrowser.OpenFilePanel("Select Project file...", dc.ddirec, "prj", false);
    try { if(TSA[0] != null) filename = TSA[0];}
    catch(System.IndexOutOfRangeException){}
  }
  IEnumerator Load(bool l) {
    AsyncOperation a = SceneManager.LoadSceneAsync(1);
    yield return new WaitUntil(() => a.isDone);
    yield break;
  }
  //both methods exist in case people keep a filewindow open and use copy paste from there
  public void SetCD(string d) {if (d != "") dc.direc = d;}
  public void SetCD() {
    string[] TSA = StandaloneFileBrowser.OpenFolderPanel("Set Project Directory...",dc.ddirec,false);
    try {if (TSA[0] != null) {
        SetCD(TSA[0] + "\\");
        direcbox.text = TSA[0] + "\\";
      }
    }
    catch(System.IndexOutOfRangeException) {}
  }
  public void SetFilename() {
    string[] TSA = StandaloneFileBrowser.OpenFilePanel("Select Project File...",dc.ddirec,"prj",false);
    try{if (TSA[0] != null) filename = TSA[0];}
    catch(System.IndexOutOfRangeException) {}
  }
  public void UpdateText() {
    creation.text = dc.direc + filename;
    loadbox.text = filename;
  }
  #endregion
  #region config
  //config is stored in the same directory as the executable as a json file
  public void SetMVol(float f) {dc.o.mVol = f;}
  public void SetFXVol(float f) {dc.o.fxVol = f;}
  public void SetMUVol(float f) {dc.o.muVol = f;}
  public void SetAVol(float f) {dc.o.aVol = f;}
  public void SetFS(bool b) {
    dc.o.f = b; 
    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, b, Screen.currentResolution.refreshRate);
  }
  public void SaveConfig() {StartCoroutine(WriteConfig());}
  public IEnumerator WriteConfig() {
    FileStream file = new FileStream(foldercheck.Parent.FullName + @"\config.json", FileMode.Create, FileAccess.Write);
    Task t = JsonSerializer.SerializeAsync<Options>(file,dc.o);
    yield return new WaitUntil(() => t.IsCompleted);
    file.Close();
    yield break;
  }
  public IEnumerator LoadConfig() {
    FileStream file = new FileStream(foldercheck.Parent.FullName + @"\config.json", FileMode.Open, FileAccess.Read);
    ValueTask<Options> t = JsonSerializer.DeserializeAsync<Options>(file);
    yield return new WaitUntil(() => t.IsCompleted);
    dc.o = t.Result;
    file.Close();
    Screen.fullScreen = dc.o.f;
    fs.isOn = dc.o.f;
    sliders[0].value = dc.o.mVol;
    sliders[1].value = dc.o.muVol;
    sliders[2].value = dc.o.fxVol;
    sliders[3].value = dc.o.aVol;
    yield break;
  }
  #endregion
    public void Leave(){Application.Quit();}
}

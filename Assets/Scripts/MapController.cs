using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HMTac;

public class MapController : MonoBehaviour { 
  #region datastore
  public HMp map;
  public DataContainer dc;
  public GameObject[,] grid;
  public List<GameObject> multiSelect, tempSelectList;
  public GameObject selector, testMenu, buttonMenu, camControl, selected;
  public List<GameObject> extras;
  public GameCamera gameCam;
  public int xSel, zSel, rad, camRange;
  public float fbShift, lrShift, scale;
  public Vector3 scalevec;
  public Material halfblue;
  public TextMeshProUGUI textBox;
  public Slider[] vols;
  public Toggle fs;
  public List<KeyValuePair<int,Paint>> palette;
  public bool s;
  private bool u;
  #endregion
  //under normal circumstances I'd put a pointer here in the interface
  void Awake() {dc = GameObject.FindObjectOfType<DataContainer>();}
  void Start() {
    xSel = 0;
    zSel = 0;
    u = true;
    SetRad(0);
    s = false;
    scalevec = new Vector3(scale, scale, scale);
    vols[0].value = dc.o.mVol;
    vols[1].value = dc.o.muVol;
    vols[2].value = dc.o.fxVol;
    vols[3].value = dc.o.aVol;
    fs.isOn = dc.o.f;
  }
  #region mapinstantiate
  public void Generate() {StartCoroutine(this.GetComponentInChildren<MapGenerator>(false).Generate(0));}
  public void FinishEGen() {
    EditorControl ec = camControl.GetComponent<EditorControl>();
    Select();
    foreach(GameObject e in grid) {
      e.GetComponent<MapTileObject>().SetupAdjacencyTable();
    }
    /*gameCam.xFoc = xSel;
    gameCam.zFoc = zSel;
    focus();*/
    //gameCam is currently unimplemented
    camControl.GetComponent<Transform>().position.Set(0,100,0);
    ec.gridspace = fbShift;
    ec.d = true;
    ec.Save();
    testMenu.SetActive(true); buttonMenu.SetActive(true);
    this.GetComponentInChildren<MapGenerator>().gameObject.SetActive(false);
  }
  public void FinishELoad() {
    Select();
    foreach(GameObject e in grid) {
      e.GetComponent<MapTileObject>().SetupAdjacencyTable();
    }
    /*gameCam.xFoc = xSel;
    gameCam.zFoc = zSel;
    focus();*/
    camControl.GetComponent<Transform>().position.Set(0,100,0);
    camControl.GetComponent<EditorControl>().gridspace = fbShift;
    testMenu.SetActive(true); buttonMenu.SetActive(true);
    this.GetComponentInChildren<MapLoader>().gameObject.SetActive(false);
  }
    #endregion
  void Update() {
    //these all reference the gameCam
    //TODO: gameCam
    /*if(Input.GetKeyDown(KeyCode.W)){forwardSelect();}
    if(Input.GetKeyDown(KeyCode.A)){leftSelect();}
    if(Input.GetKeyDown(KeyCode.S)){backSelect();}
    if(Input.GetKeyDown(KeyCode.D)){rightSelect();}*/
    if(Input.GetKeyDown(KeyCode.F11)) {
      Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, !Screen.fullScreen, Screen.currentResolution.refreshRate);
      dc.o.f = Screen.fullScreen;
      fs.isOn = Screen.fullScreen;
    }
  }
  #region cameramovement
  //meant for use with gamepads
  void ForwardSelect() {
    int dir = (gameCam.currCam + 3) % 6;
    try {
      selected = selected.GetComponent<MapTileObject>().Adjacent((byte)dir)[0];
      u = true;
      ShiftFB(dir);
    }
    catch(System.IndexOutOfRangeException) {}
  }
  void RightSelect() {
    int dir = (gameCam.currCam + 5) % 6;
    if(u) {
      dir = (dir + 5) % 6; u = false;
    }
    else u = true;
    try {
      selected = selected.GetComponent<MapTileObject>().Adjacent((byte)dir)[0];
      ShiftLR(dir);
    }
    catch(System.IndexOutOfRangeException){}
  }
  void BackSelect() {
    try {
      selected = selected.GetComponent<MapTileObject>().Adjacent(gameCam.currCam)[0];
      u = true;
      ShiftFB(gameCam.currCam);
    }
    catch(System.IndexOutOfRangeException) {}
  }
  void LeftSelect() {
    int dir = (gameCam.currCam + 1) % 6;
    if(u) {
      dir = (dir + 1) % 6; u = false;
    }
    else u = true;
    try {
      selected = selected.GetComponent<MapTileObject>().Adjacent((byte)dir)[0];
      ShiftLR(dir);
    }
    catch(System.IndexOutOfRangeException) {}
  }
  void ShiftFB(int d) {
    if (Mathf.Abs((gameCam.xFoc + gameCam.zFoc)-(xSel + zSel)) > camRange) {
      camControl.transform.Translate((gameCam.fbdirections[d] * fbShift));
      Focus(d);
    }
  }
  void ShiftLR(int d) {
    if(Mathf.Abs((gameCam.xFoc + gameCam.zFoc)-(xSel + zSel)) > camRange) {
      camControl.transform.Translate((gameCam.lrdirections[d] * lrShift));
      Focus(d);
    }
  }
  //for gameCam, needs implementation
  public void Focus() {
    Vector3 pos = grid[gameCam.xFoc,gameCam.zFoc].transform.position;
    camControl.transform.SetPositionAndRotation(new Vector3(
      pos.x, 
      camControl.transform.position.y,
      pos.z),
    camControl.transform.rotation);
  }
  public void Focus(int d) {
    GameObject n = grid[gameCam.xFoc, gameCam.zFoc].GetComponent<MapTileObject>().Adjacent((byte)d)[0];
    MapTileObject newFoc = n.GetComponent<MapTileObject>();
    gameCam.xFoc = newFoc.xVal; gameCam.zFoc = newFoc.zVal;
  }
  public void Focus(GameObject point) {camControl.transform.position = point.transform.position;}
  #endregion
  #region tileselection
  public void Select() {
    selected = grid[xSel,zSel];
    selector.transform.position = selected.transform.position + new Vector3(0,5,0);
    textBox.text = ("(" + xSel + "," + selected.GetComponent<MapTileObject>().yVal + "," + zSel + ")");
    u = true;
  }
  //this more direct version is used by mouse input
  public void Select(GameObject pick) {
    selector.SetActive(true);
    selected = pick;
    MapTileObject mto = selected.GetComponent<MapTileObject>(); 
    xSel =  mto.xVal;
    zSel =  mto.zVal;
    selector.transform.position = selected.transform.position + new Vector3(0,5,0);
    textBox.text = ("(" + xSel + "," + mto.yVal + "," + zSel + ")");
  }
  //preventing multireferencing, in case it causes issues
  public void ManySelect(GameObject pick) {
    selector.SetActive(false); s = true;
    if (!(multiSelect.Contains(pick))) {
      multiSelect.Add(pick);
      pick.GetComponent<Renderer>().materials = new Material[] {pick.GetComponent<Renderer>().material, halfblue};
    }
  }
  public void Deselect(GameObject pick) {
    if (multiSelect.Contains(pick)) multiSelect.Remove(pick);
    pick.GetComponent<Renderer>().materials = new Material[] {pick.GetComponent<Renderer>().material};
  }
  public void Deselect(List<GameObject> set) {foreach(GameObject e in set) Deselect(e);}
  public void TempSelect(GameObject pick) {tempSelectList.Add(pick);}
  public void TempDeselect(GameObject pick) {tempSelectList.Remove(pick);}
  //may change to graph traversal in engine
  public void SetRad(float r) {rad = Mathf.FloorToInt(r);}
  /* The principles within:
    Fundamentally speaking a Hexagon can be treated as 6 triangles, whose points converge on the center.
    This algorithm builds each of those triangles, bubbling out from the center.
    Note: There is an oversight where if the selection occurs near an edge, one of the triangles that should build doesn't
  */
  public void TriangleSelect(int radius, GameObject center, out List<GameObject> output) {
    output = new List<GameObject>();
    output.Add(center);
    List<GameObject> temp = new List<GameObject>();
    GameObject tgo = null;
    for (byte i = 0; i < 6; i++) {
      temp.Clear();
      try {
        tgo = center.GetComponent<MapTileObject>().Adjacent(i)[0];
        temp.Add(tgo);  
        output.Add(tgo);
      }
      catch(System.IndexOutOfRangeException){}
      for(int r = 1; r < radius; r++) {
        temp = TriBuild(i ,temp);
        foreach(GameObject e in temp) output.Add(e);
      }
    }
  }
  /*The algorithm is as such:
    For each point in your inital collection, travel "outwards" from that point, and add those to a collection.
    For the last point in that collection, add an additional point that is "rightwards" from that point.
    Repeat the operation with the new collection.  */
  List<GameObject> TriBuild(byte dir, List<GameObject> buildFrom) {
    List<GameObject> ret = new List<GameObject>();
    GameObject a = null;
    foreach(GameObject e in buildFrom) {
      try {
        a = e.GetComponent<MapTileObject>().Adjacent(dir)[0];
        ret.Add(a);
      }
      catch(System.ArgumentOutOfRangeException){}
    }
    if (a != null) {
      try {
        a = a.GetComponent<MapTileObject>().Adjacent((byte)((dir + 2) % 6))[0];
        ret.Add(a);
      }
      catch(System.ArgumentOutOfRangeException){}
    }
    return ret;
  }
  //Since everything tracks its own set of neighbors, we can rely on them to find paths to each other one.
  //Even around corners, when it comes to that.
  public List<GameObject> RadialSelect(int radius, GameObject center) {
    List<GameObject> r = new List<GameObject>(), temp;
    int i, j, k = 0, l;
    byte d;
    r.Add(center);
    for (i = 0; i < radius; i++) {
      j = r.Count;
      while (k < j) {
        if (!r[k].GetComponent<MapTileObject>().walk) {
          for (d = 0; d < 6; d++) {
            temp = r[k].GetComponent<MapTileObject>().Adjacent(d);
            for (l = 0; l < temp.Count; l++) {
              if (!r.Contains(temp[l])) r.Add(temp[l]);
            }
          }
          r[k].GetComponent<MapTileObject>().walk = true;
        }
      k++;
      }
    }
    foreach (GameObject e in r) e.GetComponent<MapTileObject>().walk = false;
    return r;
  }
  /*
  Attempting a depth-first, recursive type search has lead to problems.
  public void RadialWalk(int radius, GameObject center, ref List<GameObject> output){
    MapTileObject t = center.GetComponent<MapTileObject>();
    List<GameObject> temp;
    if(!t.walk){
      t.walk = true;
      output.Add(center);
      if(radius > 0){
        for(byte i = 0; i < 6; i++){
          temp = t.Adjacent(i);
          for(int j = 0; j < temp.Count; j++){
            if(!temp[j].GetComponent<MapTileObject>().walk){
              RadialWalk(radius - 1, temp[j], ref output);
    }}}}}
    t.walk = false;
  }
  */
  //set multiselect from tempselect, display selected tiles
  //TODO: more interesting effect in game engine
  public void FinalizeSelect() {
    selector.SetActive(false);
    if (s) {
      foreach (GameObject e in tempSelectList) {
       if (multiSelect.Contains(e)) multiSelect.Remove(e);
      }
    }
    foreach (GameObject tile in tempSelectList) multiSelect.Add(tile);
    tempSelectList.Clear();
    camControl.GetComponent<EditorControl>().selRect.SetActive(false);
    foreach (GameObject tile in multiSelect) {
      tile.GetComponent<Renderer>().materials = new Material[] {tile.GetComponent<MapTileObject>().p.mat, halfblue};
    }
    s = true;
  }
  public void ClearSelect() {
    s = false;
    foreach (GameObject e in multiSelect) e.GetComponent<Renderer>().materials = new Material[] {e.GetComponent<MapTileObject>().p.mat};
    multiSelect = new List<GameObject>();
  }
  #endregion
  #region mapchange
  //these are here, in the event the map itself changes in engine
  //using return values here to set new maxheights, where relevant
  public float ChangeHeight(GameObject pick, int h) {
    MapTileObject tile = pick.GetComponent<MapTileObject>();
    tile.ChangeHeight(h);
    if (pick.transform.position.y == 0) h = Mathf.Max(0,h);
    pick.transform.Translate(Vector3.up * (h * (scale / 2)), Space.World);
    selector.transform.position = selected.transform.position + new Vector3(0,5,0);
    textBox.text = ("(" + xSel + "," + tile.yVal + "," + zSel + ")");
    return pick.transform.position.y;
  }
  public float ChangeHeight(List<GameObject> set, int h) {
    float ret = -1;
    foreach (GameObject e in set) ret = Mathf.Max(ChangeHeight(e, h), ret);
    return ret;
  }
  public void Remove(GameObject pick) {
    pick.GetComponent<MapTileObject>().act = false;
    pick.GetComponent<MapTileObject>().yVal = 0;
    pick.transform.position = new Vector3(pick.transform.position.x, 0, pick.transform.position.z);
    pick.GetComponent<Renderer>().materials = new Material[] {halfblue};
  }
  public void Remove(List<GameObject> set) {foreach (GameObject e in set) Remove(e);}
  public void Restore(GameObject pick, Paint p) {
    pick.GetComponent<MapTileObject>().act = true;
    pick.GetComponent<MapTileObject>().ApplyPaint(p);
  }
  public void Restore(List<GameObject> sel, Paint p) {foreach (GameObject e in sel) Restore(e, p);}
  public void Create(GameObject spot, Paint p) {
    GameObject r = GameObject.Instantiate(spot, 
      spot.GetComponent<Transform>().position, 
      spot.GetComponent<Transform>().rotation, 
      this.gameObject.GetComponent<Transform>());
    extras.Add(r);
    r.GetComponent<Transform>().Translate(0,10 * scale,0,Space.World);
    r.GetComponent<MapTileObject>().ApplyPaint(p); 
    r.GetComponent<MapTileObject>().yVal += 10;
  }
  public void Create(List<GameObject> zone, Paint p) {foreach (GameObject e in zone)Create(e, p);}
  #endregion
  #region config
  //settings pile
  public void SetMVol(float f) {dc.o.mVol = f;}
  public void SetMuVol(float f) {dc.o.muVol = f;}
  public void SetFXVol(float f) {dc.o.fxVol = f;}
  public void SetAVol(float f) {dc.o.aVol = f;}
  public void SetFS(bool b) {Screen.SetResolution(
    Screen.currentResolution.width, 
    Screen.currentResolution.height, 
    b, 
    Screen.currentResolution.refreshRate); 
    dc.o.f = b;
  }
  public void SaveSettings() {StartCoroutine(saveset());}
  public IEnumerator saveset() {
    DirectoryInfo folder = new DirectoryInfo(Application.dataPath);
    FileStream file = new FileStream(folder.Parent.FullName + @"\config.json",FileMode.Create,FileAccess.Write,FileShare.Write);
    Task t = JsonSerializer.SerializeAsync<Options>(file,dc.o);
    yield return new WaitUntil(() => t.IsCompleted);
    file.Close();
    yield break;
  }
  #endregion
  //always have some in-interface way to eject
  public void Leave() {Application.Quit();}
}
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HMTac;

public class MapController : MonoBehaviour
{
  public hmp map;
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
  public List<KeyValuePair<int,paint>> palette;
  public bool s;
  private bool u;
  void Awake(){dc = GameObject.FindObjectOfType<DataContainer>();}
  void Start(){
    testMenu.SetActive(false);
    buttonMenu.SetActive(false);
    xSel = 0;
    zSel = 0;
    u = true;
    setRad(0);
    s = false;
    scalevec = new Vector3(scale, scale, scale);
    vols[0].value = dc.o.mVol;
    vols[1].value = dc.o.muVol;
    vols[2].value = dc.o.fxVol;
    vols[3].value = dc.o.aVol;
    fs.isOn = dc.o.f;}
  public void Generate(){StartCoroutine(this.GetComponentInChildren<MapGenerator>(false).Generate(0));}
  public void finishGen(){EditorControl ec = camControl.GetComponent<EditorControl>();
    select();
    foreach(GameObject e in grid)
      {e.GetComponent<MapTileObject>().setupAdjacencyTable();}
    /*gameCam.xFoc = xSel;
    gameCam.zFoc = zSel;
    focus();*/
    //gameCam is currently unimplemented
    camControl.GetComponent<Transform>().position.Set(0,100,0);
    ec.gridspace = fbShift;
    ec.d = true;
    ec.save();
    testMenu.SetActive(true); buttonMenu.SetActive(true);
    this.GetComponentInChildren<MapGenerator>().gameObject.SetActive(false);
  }
  public void finishELoad(){select();
    foreach(GameObject e in grid)
      {e.GetComponent<MapTileObject>().setupAdjacencyTable();}
    /*gameCam.xFoc = xSel;
    gameCam.zFoc = zSel;
    focus();*/
    camControl.GetComponent<Transform>().position.Set(0,100,0);
    camControl.GetComponent<EditorControl>().gridspace = fbShift;
    testMenu.SetActive(true); buttonMenu.SetActive(true);
    this.GetComponentInChildren<MapLoader>().gameObject.SetActive(false);}
    // Update is called once per frame
  void Update()
  {
    //these all reference the gameCam
    //TODO: gameCam
    /*if(Input.GetKeyDown(KeyCode.W)){forwardSelect();}
    if(Input.GetKeyDown(KeyCode.A)){leftSelect();}
    if(Input.GetKeyDown(KeyCode.S)){backSelect();}
    if(Input.GetKeyDown(KeyCode.D)){rightSelect();}*/
    if(Input.GetKeyDown(KeyCode.F11)){Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, !Screen.fullScreen, Screen.currentResolution.refreshRate); dc.o.f = Screen.fullScreen; fs.isOn = Screen.fullScreen;}
  }
  //meant for use with gamepads
  void forwardSelect()
    {int dir = (gameCam.currCam + 3) % 6;
    try{selected = selected.GetComponent<MapTileObject>().adjacent((ushort)dir)[0];
    u = true;
    shiftFB(dir);}
    catch(System.IndexOutOfRangeException){}}
  void rightSelect()
    {int dir = (gameCam.currCam + 5) % 6;
    if(u){dir = (dir + 5) % 6; u = false;}
    else{u = true;}
    try{selected = selected.GetComponent<MapTileObject>().adjacent((ushort)dir)[0];
    shiftLR(dir);}
    catch(System.IndexOutOfRangeException){}}
  void backSelect()
    {try{selected = selected.GetComponent<MapTileObject>().adjacent((ushort)gameCam.currCam)[0];
    u = true;
    shiftFB((ushort)gameCam.currCam);}
    catch(System.IndexOutOfRangeException){}}
  void leftSelect()
    {int dir = (gameCam.currCam + 1) % 6;
    if(u){dir = (dir + 1) % 6; u = false;}
    else{u = true;}
    try{selected = selected.GetComponent<MapTileObject>().adjacent((ushort)dir)[0];
    shiftLR(dir);}
    catch(System.IndexOutOfRangeException){}}
  public void setX(float x){xSel = Mathf.FloorToInt(x);}
  public void setZ(float z){zSel = Mathf.FloorToInt(z);}
  public void setRad(float r){rad = Mathf.FloorToInt(r);}
  public void select()
  {
    selected = grid[xSel,zSel];
    selector.transform.position = selected.transform.position + new Vector3(0,5,0);
    textBox.text = ("(" + xSel + "," + selected.GetComponent<MapTileObject>().yVal + "," + zSel + ")");
    u = true;
  }
  //this more direct version is used by mouse input
  public void select(GameObject pick)
  {
    selector.SetActive(true);
    selected = pick;
    MapTileObject mto = selected.GetComponent<MapTileObject>(); 
    xSel =  mto.xVal;
    zSel =  mto.zVal;
    selector.transform.position = selected.transform.position + new Vector3(0,5,0);
    textBox.text = ("(" + xSel + "," + mto.yVal + "," + zSel + ")");
  }
  //preventing multireferencing, in case it causes issues
  public void manySelect(GameObject pick){selector.SetActive(false); s = true;
    if(!(multiSelect.Contains(pick))){multiSelect.Add(pick);
    pick.GetComponent<Renderer>().materials = new Material[] {pick.GetComponent<Renderer>().material, halfblue};}}
  public void deselect(GameObject pick){if(multiSelect.Contains(pick)){multiSelect.Remove(pick);}
    pick.GetComponent<Renderer>().materials = new Material[] {pick.GetComponent<Renderer>().material};}
  public void deselect(List<GameObject> set){foreach(GameObject e in set){deselect(e);}}
  public void tempSelect(GameObject pick){tempSelectList.Add(pick);}
  public void tempDeselect(GameObject pick){tempSelectList.Remove(pick);}
  //using return values here to set new maxheights, where relevant
  public float changeHeight(GameObject pick, int h){MapTileObject tile = pick.GetComponent<MapTileObject>();
    tile.changeHeight(h);
    if(pick.transform.position.y == 0){h = Mathf.Max(0,h);} pick.transform.Translate(Vector3.up * (h * (scale / 2)), Space.World); selector.transform.position = selector.transform.position = selected.transform.position + new Vector3(0,5,0);
    textBox.text = ("(" + xSel + "," + tile.yVal + "," + zSel + ")");
    return pick.transform.position.y;}
  public float changeHeight(List<GameObject> set, int h){float ret = -1;
    foreach(GameObject e in set){ret = Mathf.Max(changeHeight(e, h), ret);}
    return ret;}
  //may change to graph traversal in engine
  public void radialSelect(){radialSelect(rad, selected, out tempSelectList);
  finalizeSelect();}
  /* The principles within:
    Fundamentally speaking a Hexagon can be treated as 6 triangles, whose points converge on the center.
    This algorithm builds each of those triangles, bubbling out from the center.*/
  public void radialSelect(int radius, GameObject center, out List<GameObject> output){output = new List<GameObject>();
    output.Add(center);
    List<GameObject> temp = new List<GameObject>();
    GameObject tgo = null;
    for(ushort i = 0; i < 6; i++)
      {temp.Clear();
      try{tgo = center.GetComponent<MapTileObject>().adjacent(i)[0];
        temp.Add(tgo);  
        output.Add(tgo);}
      catch(System.IndexOutOfRangeException){}
        for(int r = 1; r < radius; r++)
        {temp = triBuild(i ,temp);
        foreach(GameObject e in temp){output.Add(e);}}}}
  List<GameObject> triBuild(ushort dir, List<GameObject> buildFrom){List<GameObject> ret = new List<GameObject>();
    GameObject a = null;
    foreach(GameObject e in buildFrom)
      {try{a = e.GetComponent<MapTileObject>().adjacent(dir)[0];
        ret.Add(a);}
      catch(System.ArgumentOutOfRangeException){}}
      if(a != null){try{a = a.GetComponent<MapTileObject>().adjacent((ushort)((dir + 2) % 6))[0]; ret.Add(a);}
      catch(System.ArgumentOutOfRangeException){}}
      return ret;
  }
  //set tempselect, display selected tiles
  //TODO: more interesting effect in game engine
  public void finalizeSelect()
  {selector.SetActive(false);
  if(s){foreach(GameObject e in tempSelectList)
  {if(multiSelect.Contains(e))
  {multiSelect.Remove(e);}}}
    foreach(GameObject tile in tempSelectList){multiSelect.Add(tile);} tempSelectList.Clear(); camControl.GetComponent<EditorControl>().selRect.SetActive(false);
    foreach(GameObject tile in multiSelect){tile.GetComponent<Renderer>().materials = new Material[] {tile.GetComponent<MapTileObject>().p.mat, halfblue};}
    s = true;}
  void shiftFB(int d)
  {if(Mathf.Abs((gameCam.xFoc + gameCam.zFoc)-(xSel + zSel)) > camRange)
    {camControl.transform.Translate((gameCam.fbdirections[d] * fbShift));
    focus(d);}}
  void shiftLR(int d){
    EditorControl cam = camControl.GetComponent<EditorControl>();
    if(Mathf.Abs((gameCam.xFoc + gameCam.zFoc)-(xSel + zSel)) > camRange)
    {camControl.transform.Translate((gameCam.lrdirections[d] * lrShift));
    focus(d);}}
  public void clearSelect(){s = false;
    foreach(GameObject e in multiSelect){e.GetComponent<Renderer>().materials = new Material[] {e.GetComponent<MapTileObject>().p.mat};}
    multiSelect = new List<GameObject>();}
  //for gameCam, needs implementation
  public void focus()
    {Vector3 pos = grid[gameCam.xFoc,gameCam.zFoc].transform.position;
    camControl.transform.SetPositionAndRotation(new Vector3(pos.x, camControl.transform.position.y,pos.z),camControl.transform.rotation);}
  public void focus(int d)
    {GameObject n = grid[gameCam.xFoc, gameCam.zFoc].GetComponent<MapTileObject>().adjacent((ushort)d)[0];
    MapTileObject newFoc = n.GetComponent<MapTileObject>();
    gameCam.xFoc = newFoc.xVal; gameCam.zFoc = newFoc.zVal;}
  public void focus(GameObject point){camControl.transform.position = point.transform.position;}
  //these are here, in the event the map itself changes in engine
  public void remove(GameObject pick){pick.GetComponent<MapTileObject>().act = false; pick.GetComponent<MapTileObject>().yVal = 0; pick.transform.position = new Vector3(pick.transform.position.x, 0, pick.transform.position.z); pick.GetComponent<Renderer>().materials = new Material[] {halfblue};}
  public void remove(List<GameObject> set){foreach(GameObject e in set){remove(e);}}
  public void restore(GameObject pick, paint p){pick.GetComponent<MapTileObject>().act = true; pick.GetComponent<MapTileObject>().applyPaint(p);}
  public void restore(List<GameObject> sel, paint p){foreach(GameObject e in sel){restore(e, p);}}
  public void create(GameObject spot, paint p){GameObject r = GameObject.Instantiate(spot, spot.GetComponent<Transform>().position, spot.GetComponent<Transform>().rotation, this.gameObject.GetComponent<Transform>());
  extras.Add(r); r.GetComponent<Transform>().Translate(0,10 * scale,0,Space.World); r.GetComponent<MapTileObject>().applyPaint(p); r.GetComponent<MapTileObject>().yVal += 10;}
  public void create(List<GameObject> zone, paint p){foreach(GameObject e in zone){create(e, p);}}
  //settings pile
  public void SetMVol(float f){dc.o.mVol = f;}
  public void SetMuVol(float f){dc.o.muVol = f;}
  public void SetFXVol(float f){dc.o.fxVol = f;}
  public void SetAVol(float f){dc.o.aVol = f;}
  public void setFS(bool b){Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, b, Screen.currentResolution.refreshRate); dc.o.f = b;}
  public void SaveSettings(){StartCoroutine(saveset());}
  public IEnumerator saveset(){DirectoryInfo folder = new DirectoryInfo(Application.dataPath);
    FileStream file = new FileStream(folder.Parent.FullName + @"\config.json",FileMode.Create,FileAccess.Write,FileShare.Write);
    Task t = JsonSerializer.SerializeAsync<options>(file,dc.o);
    yield return new WaitUntil(() => t.IsCompleted);
    file.Close();
    yield break;}
  //always have some in-interface way to eject
  public void leave(){Application.Quit();}
}
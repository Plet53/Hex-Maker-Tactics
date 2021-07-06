using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Text.Json;
using System.Threading.Tasks;
using TMPro;
using SFB;
using HMTac;

public class EditorControl : MonoBehaviour
{
  #region datastore
  public GameObject cam, canv, selRect, dispSelRect, radDisp, paintMenu, paintScroll, paintCreate, previewTex, previewTile, dne, pnv, e, newButton, genmenu, loadmapmenu, newprjmenu, loadprjmenu, savebox, topbar, lprje;
  public Paint paint;
  public MapController control;
  public Camera previewCam;
  public RenderTexture preview, paintpre;
  public float maxHeight, gridspace, canvtogrid, camangleadjust;
  public int rad;
  public int flag {get; set;}
  public int currMap {get; set;}
  public ushort newX, newZ;
  public bool d, donesave;
  public BitArray toolset, tflags;
  public Material bmat, tmat, imat;
  public List<GameObject> paintlist;
  public List<Paint> palette;
  public TMP_Dropdown loadlist;
  public Vector4 tc;
  public TMP_InputField path, loadpath, namebox;
  public TextMeshProUGUI bartext, newmenutext;
  public Prj tprj;
  public string filename {get; set;}
  public string texname {get; set;}
  public static string paints = "paints/", previews = "previews/", maps = "maps/", sounds = "sounds/", music = "music/", charsheet = "character_sheets/", models = "models/";
  private Vector2 initmp, mp, p, tangle, mirror;
  private Vector3 pos, size, fw, ri;
  private Texture2D tex;
  private RaycastHit hit;
  private Transform camtran;
  private RectTransform rt, crt;
  private bool firstHit, camMode;
  #endregion
  IEnumerator Start()
  {
    //intialization pile
    maxHeight = 0;
    pos = Vector3.zero;
    size = new Vector3(0,maxHeight,0);
    mp = Vector2.zero;
    p = Vector2.zero;
    tangle = Vector2.zero;
    mirror = Vector2.zero;
    fw = Vector3.forward;
    ri = Vector3.right;
    //for map previews using GameCamera, TODO: Implement that
    camMode = true;
    camtran = cam.GetComponent<Transform>();
    //Default toolset is single select
    toolset = new BitArray(8);
    //Default tools are: Single Select
    toolset[0] = true; toolset[3] = true;
    paintlist = new List<GameObject>();
    tflags = new BitArray(8, false);
    tc = new Vector4(1,1,1,1);
    texname = "unnamed";
    //Camera Angle Adjust, for converting screenspace rect to flattened rect on the grid
    camangleadjust = Mathf.Abs(Mathf.Sin(Mathf.Deg2Rad * camtran.eulerAngles.x));
    canvtogrid = cam.GetComponent<Camera>().orthographicSize / 400f;
    control.palette = new List<KeyValuePair<int, Paint>>();
    palette = new List<Paint>();
    //We're going to need this a lot, store references for later
    rt = dispSelRect.GetComponent<RectTransform>();
    crt = canv.GetComponent<RectTransform>();
    //Assume we're loading a project with a saved map
    if(control.dc.l && control.dc.p.mapcount > 0){loadmapmenu.SetActive(true); control.GetComponentInChildren<MapLoader>(true).gameObject.SetActive(true);
    List<string> l = new List<string>(control.dc.p.mapcount); 
    for(int i = 0; i < control.dc.p.mapcount; i++){l.Add(control.dc.p.names[i]);
    currMap = control.dc.p.lastmap;}
    loadlist.AddOptions(l);
    yield return LoadPalette();
    //print(palette.Count);
    }
    //Assume we aren't
    else{StartCoroutine(SavePrj());
    genmenu.SetActive(true); control.GetComponentInChildren<MapGenerator>(true).gameObject.SetActive(true);
    loadlist.value = control.dc.p.lastmap + 1;
    loadlist.RefreshShownValue();
    currMap = control.dc.p.lastmap;}
    //TODO: Implement project editing within interface
    //Namely: Map and paint deletion, renaming of project as a whole
    tprj = control.dc.p;
    if(palette.Count == 0){paint = new Paint();
    paint.flagset = tflags;
    paint.mat = imat;
    paint.name = "Default";
    paint.color = tc;
    palette.Add(paint);
    BuildButton(paint);
    StartCoroutine(SavePaint(paint));
    StartCoroutine(SaveImage(control.dc.direc + paints + paint.name + ".png", (Texture2D)imat.mainTexture));}
    StartCoroutine(AniPreview());
    d = false;
    UpdateBar();
    //break coroutine, free up those resources
    yield break;
  }
  void Update(){
    //initial click
    if(Input.GetMouseButtonDown(0)){

      initmp = Input.mousePosition;
      if(toolset[1]){
        dispSelRect.SetActive(true);
        rt.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        rt.anchorMax = Vector2.zero;}
      //when using hex rad select, immediately set center to object that was just clicked
      if(toolset[2]){
        radDisp.SetActive(true);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out hit)){control.Select(hit.collider.gameObject);
      }}
      //eraser and creator operate on intial click without modifiers, has better feel to me
      if(toolset[0]){
        if(toolset[5]){
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out hit)){
          GameObject h = hit.collider.gameObject;
          Create(h); d = true;}}
        if(toolset[7]){
          Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
          if(Physics.Raycast(ray, out hit)){
            GameObject h = hit.collider.gameObject;
            Delete(h); d = true;}
    }}}
    //during clickhold
    if(Input.GetMouseButton(0)){
      if(toolset[6]){
        mp = Input.mousePosition;
        if((Mathf.Abs(mp.y - initmp.y)) > (canvtogrid * 40)){
          if(control.s){maxHeight = Mathf.Max(control.ChangeHeight(control.multiSelect, ((initmp.y > mp.y) ? -1 : 1)),maxHeight); d = true;}
          else{maxHeight = Mathf.Max(control.ChangeHeight(control.selected, ((initmp.y > mp.y) ? -1 : 1)),maxHeight); d = true;}
          initmp.y = mp.y;
      }}
      else{if(toolset[0]){
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out hit)){
          GameObject h = hit.collider.gameObject;
          if(toolset[3]){if((Input.GetAxis("Modifier") > 0)){control.ManySelect(h);}
          else if((Input.GetAxis("Modifier") < 0)){control.Deselect(h);}}
        //multiselection is tracked, behaves similarly to image editing software
        if(toolset[4]){
          if(control.s){if(control.multiSelect.Contains(h)){h.GetComponent<MapTileObject>().ApplyPaint(paint); d = true;}}
          else{h.GetComponent<MapTileObject>().ApplyPaint(paint); d = true;}}
        if(toolset[7] && (Input.GetAxis("Modifier") > 0)){Delete(h); d = true;
        }}}
      //recttransform explicitly does not support negative sizes, re-set position as needed
        if(toolset[1]){
          mp = Input.mousePosition;
          if(mp.x >= initmp.x){mirror.x = 1;
            tangle.x = (mp.x - rt.position.x) / crt.rect.width;
            p.x = initmp.x;}
          else{mirror.x = -1;
            tangle.x = (initmp.x - mp.x) / crt.rect.width;
            p.x = mp.x;}
          if(mp.y >= initmp.y){mirror.y = 1;
            tangle.y = (mp.y - rt.position.y) / crt.rect.height;
            p.y = initmp.y;}
          else{mirror.y = -1;
            tangle.y = (initmp.y - mp.y) / crt.rect.height;
            p.y = mp.y;}
        rt.position = p;
        rt.anchorMax = tangle;
      //I've tried having this only operate on release, doesn't work, so it lives here instead
        if(firstHit){
          size.x = rt.rect.width * canvtogrid * mirror.x;
          size.z = (rt.rect.height / camangleadjust) * canvtogrid * mirror.y;
          selRect.GetComponent<BoxCollider>().size = size;
          selRect.GetComponent<BoxCollider>().center = size / 2;
        }
      else{
        initmp = mp; //we want reflection between actual behavior and what the interface reports
        Ray ray = Camera.main.ScreenPointToRay(mp);
        if(Physics.Raycast(ray, out hit)){
          selRect.SetActive(true);
          pos = hit.point - Vector3.up;
          selRect.GetComponent<Transform>().position = pos;
          selRect.GetComponent<Transform>().rotation = Quaternion.Euler(0,cam.GetComponent<Transform>().rotation.eulerAngles.y,0);
          firstHit = true;
        }}}
          if(toolset[2]){rad = Mathf.FloorToInt(Mathf.Abs((Vector2.Distance(initmp, Input.mousePosition) * canvtogrid))/10);
            radDisp.GetComponent<RectTransform>().position = new Vector3(Input.mousePosition.x - 1, Input.mousePosition.y + 1,0);
            radDisp.GetComponent<TextMeshProUGUI>().text = rad.ToString();
    }}}
    //on clickrelease
    if(Input.GetMouseButtonUp(0)){radDisp.SetActive(false);
      bool r = true;
      if(toolset[0]){
        if(toolset[3]){Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out hit)){if(Input.GetAxis("Modifier") > 0){control.ManySelect(hit.collider.gameObject);}
          else if(Input.GetAxis("Modifier") < 0){control.Deselect(hit.collider.gameObject);}
          else{control.ClearSelect(); control.Select(hit.collider.gameObject);}
      }}}
      if(toolset[1]){selRect.SetActive(false); dispSelRect.SetActive(false); firstHit = false;}
      if(toolset[2]){
        if(rad != 0){control.tempSelectList = control.RadialSelect(rad, control.selected);}
        else{r = false;}}
      if(toolset[1] || (toolset[2] && r)){
        if(toolset[3]){if(Input.GetAxis("Modifier") == 0){control.ClearSelect(); control.FinalizeSelect();}
          else if(Input.GetAxis("Modifier") < 0){control.tempSelectList = FilterList(control.tempSelectList,control.multiSelect);
          control.Deselect(control.tempSelectList);}
          else{control.FinalizeSelect();}}
        if(toolset[4]){
          if(control.s){control.tempSelectList = FilterList(control.tempSelectList,control.multiSelect);}
          UsePaint(control.tempSelectList, paint); control.tempSelectList.Clear(); d = true;}
        if(toolset[5]){
          if(control.s){
            control.tempSelectList = FilterList(control.tempSelectList,control.multiSelect);}
          Create(control.tempSelectList); control.tempSelectList.Clear(); d = true;}
        if(toolset[7]){
          if(control.s){
            control.tempSelectList = FilterList(control.tempSelectList,control.multiSelect);}
          Delete(control.tempSelectList); control.tempSelectList.Clear(); d = true;}
      }}
    //Control speed is modified by mod keys, SHIFTs increase speed, CTRLs decrease
    if((Input.GetAxis("Vertical") > 0) && camMode){
      camtran.Translate(fw * Time.deltaTime * 100 * ((Input.GetAxis("Modifier") > 0) ? 2 : (Input.GetAxis("Modifier") < 0) ? 0.5f : 1), Space.World);}
    if((Input.GetAxis("Horizontal") < 0) && camMode){
      camtran.Translate(ri * Time.deltaTime * 100 * -1 * ((Input.GetAxis("Modifier") > 0) ? 2 : (Input.GetAxis("Modifier") < 0) ? 0.5f : 1), Space.World);}
    if((Input.GetAxis("Vertical") < 0) && camMode){
      camtran.Translate(fw * Time.deltaTime * 100 * -1 * ((Input.GetAxis("Modifier") > 0) ? 2 : (Input.GetAxis("Modifier") < 0) ? 0.5f : 1), Space.World);}
    if((Input.GetAxis("Horizontal") > 0) && camMode){
      camtran.Translate(ri * Time.deltaTime * 100 * ((Input.GetAxis("Modifier") > 0) ? 2 : (Input.GetAxis("Modifier") < 0) ? 0.5f : 1), Space.World);}
    //Rotation is handled by the scroll wheel while holding CTRL
    //Otherwise scroll wheel handles "zoom"
    if(Input.mouseScrollDelta.y != 0){
      if(Input.GetAxis("Modifier") < 0){
        if(control.s){
          if(Input.mouseScrollDelta.y > 0){
            foreach(GameObject e in control.multiSelect){e.GetComponent<MapTileObject>().RotRight();}}
          else{foreach(GameObject e in control.multiSelect){e.GetComponent<MapTileObject>().RotLeft();}}}
        else{if(Input.mouseScrollDelta.y > 0){control.selected.GetComponent<MapTileObject>().RotRight();}
          else{control.selected.GetComponent<MapTileObject>().RotLeft();}}}
      else{cam.GetComponent<Camera>().orthographicSize = 
        Mathf.Clamp(cam.GetComponent<Camera>().orthographicSize - 
        (Input.mouseScrollDelta.y * 10 * 
        ((Input.GetAxis("Modifier") > 0) ? 2 : (Input.GetAxis("Modifier") < 0) ? 0.5f : 1)),
        10,200);
        canvtogrid = (float)(cam.GetComponent<Camera>().orthographicSize / 400);}
      }
    //Middle click moves the camera as well
    if(Input.GetMouseButton(2)){
      camtran.Translate(
        (Input.GetAxis("Mouse X") * -2 * ri * ((Input.GetAxis("Modifier") > 0) ? 2 : (Input.GetAxis("Modifier") < 0) ? 0.5f : 1)) 
        + (Input.GetAxis("Mouse Y") * -2 * fw * ((Input.GetAxis("Modifier") > 0) ? 2 : (Input.GetAxis("Modifier") < 0) ? 0.5f : 1)), Space.World);
      }
    //Right click rotates the camera
    if(Input.GetMouseButton(1)){
      camtran.rotation = Quaternion.Euler(
        camtran.eulerAngles.x + (Input.GetAxis("Mouse Y") * ((Input.GetAxis("Modifier") > 0) ? 2 : (Input.GetAxis("Modifier") < 0) ? 0.5f : 1)), //x
        camtran.eulerAngles.y + (Input.GetAxis("Mouse X") * ((Input.GetAxis("Modifier") > 0) ? 2 : (Input.GetAxis("Modifier") < 0) ? 0.5f : 1)), //y
        0); //z
      //camera z rotation is reserved for dutch angles
      fw.x = Mathf.Sin(camtran.eulerAngles.y * Mathf.Deg2Rad);
      fw.z = Mathf.Cos(camtran.eulerAngles.y * Mathf.Deg2Rad);
      ri.x = Mathf.Sin((camtran.eulerAngles.y + 90) * Mathf.Deg2Rad);
      ri.z = Mathf.Cos((camtran.eulerAngles.y + 90) * Mathf.Deg2Rad);
    }
    if(Input.GetMouseButtonUp(1)){camangleadjust = Mathf.Abs(Mathf.Sin(Mathf.Deg2Rad * camtran.eulerAngles.x));}
    if((Input.GetAxis("Command") != 0)){
      if(Input.GetKeyDown(KeyCode.A)){foreach (GameObject t in control.grid){control.ManySelect(t);}
        foreach(GameObject t in control.extras){control.ManySelect(t);}}
      if(Input.GetKeyDown(KeyCode.S)){Save();}
      if(Input.GetKeyDown(KeyCode.D)){control.ClearSelect();}}
    if((Input.GetAxis("Alt Modifier") != 0) && Input.GetKeyDown(KeyCode.F4)){Leave();}
  }
  #region updatehelpers
  public void Create(GameObject e){if(e.GetComponent<MapTileObject>().act){control.Create(e,paint);}else{control.Restore(e,paint);}}
  public void Create(List<GameObject> l){foreach(GameObject e in l){Create(e);}d = true;}
  public void Delete(GameObject e){if(control.extras.Contains(e)){Destroy(e);}else{control.Remove(e);}}
  public void Delete(List<GameObject> l){foreach(GameObject e in l){Delete(e);}d = true;}
  public void UsePaint(List<GameObject> l, Paint paint){
    foreach (GameObject t in l){t.GetComponent<MapTileObject>().ApplyPaint(paint);}
    }
  //was built to deal with enumeration not modifying structure it enumerates over
  public List<GameObject>FilterList(List<GameObject> target, List<GameObject> filter){
    List<GameObject> ret = new List<GameObject>();
    foreach(GameObject e in target){if(filter.Contains(e)){ret.Add(e);}}
    return ret;
  }
  #endregion
  #region interface
  //Unity lets me use these as part of a togglegroup, which handles states for me. Convenient.
  public void SetPointTool(bool state){toolset[0] = state;}
  public void SetRectTool(bool state){toolset[1] = state;}
  public void SetRadTool(bool state){toolset[2] = state;}
  public void SetSelTool(bool state){toolset[3] = state;}
  public void SetPaintTool(bool state){toolset[4] = state;}
  public void SetCreateTool(bool state){toolset[5] = state;}
  public void SetHeightTool(bool state){toolset[6] = state;}
  public void SetEraserTool(bool state){toolset[7] = state;}
  public void SetPaint(int i){paint = palette[i];}
  public void SetFlag(bool state){tflags[flag] = state;}
  public void SetR(float val){tc.x = val;}
  public void SetG(float val){tc.y = val;}
  public void SetB(float val){tc.z = val;}
  public void SetA(float val){tc.w = val;}
  public void SetNewX(string i){newX = ushort.Parse(i);}
  public void SetNewZ(string i){newZ = ushort.Parse(i);}
  public void SetNewMC(string i){tprj.mapcount = int.Parse(i);}
  public void SetNewPL(string i){tprj.palettelen = int.Parse(i);}
  public void SetWM(bool b){tprj.wm = b;}
  public void UpdateBar(){bartext.text = control.dc.p.names[currMap] + " - " + currMap + ".hmp - " + control.dc.p.name;}
  public void UpdateMenus(){newmenutext.text = control.dc.direc + filename;}
  public void Save(){if(d){StartCoroutine(Saving());}}
  public IEnumerator SaveDialog(){if(d){donesave = false;
    savebox.SetActive(true);
    yield return new WaitUntil(() => donesave);}
    if(d){yield break;}
  }
    public IEnumerator Saving(){yield return SaveMp();
    yield return SavePrj();
    donesave = true; d = false;
    yield break;}
  //used by the save dialog to break out without saving
  public void DontSave(){donesave = true; d = false;}
  public void CancelSave(){donesave = true;}
  public void ReturnToMenu(){StartCoroutine(RTM());}
  public IEnumerator RTM(){yield return SaveDialog();
  AsyncOperation a = SceneManager.LoadSceneAsync(0);
  yield return new WaitUntil(() => a.isDone);
  yield break;}

  //animates the paint previews, alter w to change framespeed
  public IEnumerator AniPreview(){
    WaitForSeconds w = new WaitForSeconds(.33f);
    int f = 0;
    while(true){
      yield return w; 
      foreach(GameObject e in paintlist){e.GetComponent<PaintBucket>().SetFrame(f); 
      f = (f + 1) % 6;
  }}}
  public void SetCD(string d){control.dc.direc = d;}
  public void SetCD(){string[] TSA = StandaloneFileBrowser.OpenFolderPanel("Set Project Directory...",control.dc.direc,false);
  try{if(TSA[0] != null){SetCD(TSA[0]);}}
  catch(System.IndexOutOfRangeException){}}
  //check save on exit attempts as well, but not if external window button is used
  public void Leave(){StartCoroutine(Exit());}
  public IEnumerator Exit(){yield return SaveDialog();
    if(!d){Application.Quit();}}
  #endregion
  #region buttonmanage
  public void BuildButton(Paint p){
    RectTransform psrt = paintScroll.GetComponent<RectTransform>();
    //building a scrolling menu of buttons
    psrt.offsetMin = new Vector2(psrt.offsetMin.x,psrt.offsetMin.y - 44);
    GameObject b = Instantiate<GameObject>(newButton,new Vector3(psrt.position.x,psrt.position.y + 13 - (22 * p.index),psrt.position.z),Quaternion.identity,psrt);
    paintlist.Add(b);
    PaintBucket pb = b.GetComponent<PaintBucket>();
    pb.index = p.index;
    pb.e = this;
    pb.t.text = p.name;
    pb.DispFlags();
    StartCoroutine(BuildPreview(b.GetComponent<PaintBucket>()));
  }
  private IEnumerator BuildPreview(PaintBucket pb){previewTile.SetActive(true); previewCam.gameObject.SetActive(true);
    Vector3 sixths = new Vector3(0,60,0);
    previewTile.GetComponent<Renderer>().material = palette[pb.index].mat;
    pb.preview = new Texture2D(120,72);
    pb.preview.filterMode = FilterMode.Point;
    //two loops to track tiny rectangle
    //probably doesn't affect filespace but looks cleaner
    for(int j = 0; j < 2; j++){
      for(int k = 0; k < 3; k++){
        previewCam.Render();
        //copy rendertextrue to rendertexture
        Graphics.CopyTexture(preview,0,0,0,0,40,36,paintpre,0,0,(40*k),(36*j));
        //spin
        previewTile.GetComponent<Transform>().Rotate(sixths, Space.World);}}
    previewTile.SetActive(false); previewCam.gameObject.SetActive(false);
    Graphics.SetRenderTarget(paintpre);
    //readpixels is the only way to get GPU data back to the CPU
    //little clunky but it works
    pb.preview.ReadPixels(new Rect(0,0,120,72),0,0,true);
    pb.preview.Apply();
    //is rendering a tiny texture computationally cheap? yes
    //am I still going to save a copy to skip that process on load? also yes
    StartCoroutine(SaveImage(control.dc.direc + previews + palette[pb.index].name + ".png",pb.preview));
    pb.copy = new Texture2D(40,36);
    pb.copy.filterMode = 0;
    pb.SetFrame(0);
    pb.image.texture = pb.copy;
    yield break;
  }
  //LoadButton also loads preview, unless it doesn't exist.
  public IEnumerator LoadButton(int i){
    RectTransform psrt = paintScroll.GetComponent<RectTransform>();
    psrt.offsetMin = new Vector2(psrt.offsetMin.x, psrt.offsetMin.y - 44);
    GameObject b = Instantiate<GameObject>(newButton,new Vector3(psrt.position.x, psrt.position.y + 13 - (22 * i),psrt.position.z),Quaternion.identity,psrt);
    paintlist.Add(b);
    PaintBucket pb = b.GetComponent<PaintBucket>();
    pb.index = i;
    pb.e = this;
    pb.t.text = palette[i].name;
    pb.DispFlags();
    pb.copy = new Texture2D(40,36);
    pb.copy.filterMode = 0;
    FileInfo check = new FileInfo(control.dc.direc + previews + palette[i].name + ".png");
    if(check.Exists){OutputCoroutine<Texture2D> c = new OutputCoroutine<Texture2D>(this, LoadImage(control.dc.direc + previews + palette[i].name + ".png"));
    yield return c.c;
    pb.preview = c.r;
    pb.SetFrame(0);
    pb.image.texture = pb.copy;}
    else{yield return BuildPreview(pb);}
    yield break;
  }
  #endregion
  #region paintmanage
  public void NewPaint(){
    tmat = new Material(bmat);
    texname = "unnamed" + palette.Count;
    namebox.text = texname;
  }
  public void UpdateColor(){previewTex.GetComponent<RawImage>().color = tc;}
  public void CancelPaint(){previewTex.SetActive(false); tc.Set(1,1,1,1);}
  public void AcceptPaint(){
    previewTex.SetActive(false);
    tmat.SetTexture("_MainTex", previewTex.GetComponent<RawImage>().texture);
    Texture2D t = (Texture2D)previewTex.GetComponent<RawImage>().texture;
    StartCoroutine(SaveImage(control.dc.direc + paints + texname + ".png",t));
    tmat.color = tc;
    Paint tp = new Paint();
    tp.mat = tmat;
    tp.flagset = tflags;
    tp.name = texname;
    tp.color = tc;
    tp.index = palette.Count;
    palette.Add(tp);
    tmat = bmat;
    tmat.color = bmat.color;
    tflags = new BitArray(8, false);
    tc.Set(1,1,1,1);
    BuildButton(tp);
    StartCoroutine(SavePaint(tp));
  }
  public IEnumerator SavePaint(Paint p){
    FileStream file = new FileStream(control.dc.direc + paints + control.dc.p.palettelen + ".pnt",FileMode.Create,FileAccess.ReadWrite);
    Task t = JsonSerializer.SerializeAsync<Paint>(file, p);
    yield return new WaitUntil(() => t.IsCompleted);
    file.Close();
    control.dc.p.palettelen++;
    yield return SavePrj();
    yield break;
  }
  public IEnumerator LoadPaint(int index){
    Paint p = new Paint();
    string filename = index + ".pnt";
    FileStream file = new FileStream(control.dc.direc + paints + filename,FileMode.Open,FileAccess.Read,FileShare.Read);
    ValueTask<Paint> t = JsonSerializer.DeserializeAsync<Paint>(file);
    yield return new WaitUntil(() => t.IsCompleted);
    file.Close();
    p = t.Result;
    Material temp = new Material(bmat);
    temp.color = p.color;
    filename = p.name + ".png";
    OutputCoroutine<Texture2D> c = new OutputCoroutine<Texture2D>(this, LoadImage(control.dc.direc + paints + filename));
    yield return c.c;
    c.r.filterMode = FilterMode.Point;
    temp.SetTexture("_MainTex",c.r);
    p.mat = temp;
    palette.Add(p);
    yield return LoadButton(index);
    yield break;
  }
  public IEnumerator LoadPalette(){
    for(int i = 0; i < control.dc.p.palettelen; i++){yield return (LoadPaint(i));}
    paint = palette[0]; yield break;
  }
  //got to manually clear this as well
  public void ClearPalette(){
    foreach(GameObject e in paintlist){Destroy(e);}
    paintlist.Clear();
    palette.Clear();
    RectTransform psrt = paintScroll.GetComponent<RectTransform>();
    psrt.offsetMin = new Vector2(psrt.offsetMin.x, -100);
  }
  #endregion
  #region texmanage
  public void OpenTexture(){string[] TSA = StandaloneFileBrowser.OpenFilePanel("Select Texture...", control.dc.ddirec,"png",false);
  try{if(TSA[0]!=null){filename = TSA[0];
  path.text = filename;
  StartCoroutine(LoadTexFromFile());}}
  catch(System.IndexOutOfRangeException){}
  catch(System.IO.FileNotFoundException){dne.SetActive(true);}
  catch(System.ArgumentException){pnv.SetActive(true);}
  catch(Exception){e.SetActive(true);}}
  public void OpenTexture(string path){
    if(path != ""){try{filename = path;
    StartCoroutine(LoadTexFromFile());}
    catch(System.IO.FileNotFoundException){dne.SetActive(true);}
    catch(System.ArgumentException){pnv.SetActive(true);}
    catch(Exception){e.SetActive(true);}}
  }
  private IEnumerator LoadTexFromFile(){pnv.SetActive(false); dne.SetActive(false); e.SetActive(false);
    OutputCoroutine<Texture2D> c = new OutputCoroutine<Texture2D>(this, LoadImage(filename));
    yield return c.c;
    tex = c.r;
    tex.filterMode = FilterMode.Point;
    previewTex.GetComponent<RawImage>().texture = tex;
    filename = "";
    previewTex.SetActive(true);
    }
  #endregion
  #region mapfilemanage
  //file extensions are fake
  public IEnumerator SaveMp(){
    for(int j = 0; j < control.map.xLen; j++){
      for(int k = 0; k < control.map.grid.Length / control.map.xLen; k++){
        control.map.grid[j,k] = control.grid[j,k].GetComponent<MapTileObject>().ToCell();
    }}
    FileStream save = new FileStream(control.dc.direc + maps + currMap + ".hmp",FileMode.Create,FileAccess.ReadWrite,FileShare.ReadWrite);
    Task t = JsonSerializer.SerializeAsync<HMp>(save,control.map);
    yield return new WaitUntil(() => t.IsCompleted);
    save.Close();
    yield break;
  }
  //unity UI can only call voids
  public void NewMap(){StartCoroutine(NewMp());}
  //all options that would exit the current map go through the savedialog
  //like any good editor
  public IEnumerator NewMp(){
    yield return SaveDialog();
    currMap = control.dc.p.mapcount;
    string[] temp = new string[control.dc.p.mapcount + 1];
    control.dc.p.names.CopyTo(temp,0);
    temp[control.dc.p.mapcount] = "unnamed";
    control.dc.p.names = temp;
    ClearMap();
    genmenu.SetActive(true);
    control.GetComponentInChildren<MapGenerator>(true).gameObject.SetActive(true);
  }
  //I feel like the dropdown could have an option that just does this
  public void AddToLoadMenu(int i){
    TMP_Dropdown.OptionData od = new TMP_Dropdown.OptionData(control.dc.p.names[i]);
    loadlist.options.Add(od);
  }
  public void LoadMap(){StartCoroutine(LoadMp());}
  public IEnumerator LoadMp(){yield return SaveDialog();
    ClearMap();
    loadmapmenu.SetActive(true);
    loadlist.value = currMap;}
  public void LoadAMap(){LoadMap(currMap);}
  //orphaning objects by clearing the structure they're stored in doesn't trigger GC
  //that or the objects being children of the controller preserves a reference
  //either way
  public void ClearMap(){int zLen = control.grid.Length / control.map.xLen;
    for(int j = 0; j < control.map.xLen; j++){
      for(int k = 0; k < zLen; k++){GameObject.Destroy(control.grid[j,k]);}}
    foreach(GameObject e in control.extras){GameObject.Destroy(e);}
    control.map = new HMp();
  }
  public void UpdateMap(){StartCoroutine(UpdateMp());}
  public IEnumerator UpdateMp(){MapGenerator g = control.GetComponentInChildren<MapGenerator>(true);
    g.gameObject.SetActive(true);
    yield return g.ReGenerate(0,newX,newZ);
    control.dc.p.names[currMap] = (filename == "" ? control.dc.p.names[currMap] : filename);
    newX = 0; newZ = 0;
    filename = "";
    UpdateBar();
    }
  public void SetCurrMap(int i){currMap = i;}
  public void LoadMap(int i){control.dc.p.lastmap = i;
    currMap = i;
    MapLoader l = control.GetComponentInChildren<MapLoader>(true);
    l.gameObject.SetActive(true);
    StartCoroutine(l.ELoad(control.dc.direc + maps + i + ".hmp"));
  }
  public void NameMap(string s){control.dc.p.names[currMap] = s; d = true;}
  #endregion
  #region prjfilemanage
    public void NewProject(){StartCoroutine(NewPrj());}
  public IEnumerator NewPrj(){
    yield return SaveDialog();
    ClearMap();
    control.dc.direc = control.dc.ddirec;
    UpdateMenus();
    newprjmenu.SetActive(true);
  }
  public void MakeNewPrj(){
    filename = (filename == "" ? "unnamed" : filename);
    control.dc.direc = control.dc.direc + "\\" + filename + "\\";
    DirectoryInfo foldercheck = Directory.CreateDirectory(control.dc.direc);
    foldercheck.CreateSubdirectory("paints");
    foldercheck.CreateSubdirectory("previews");
    foldercheck.CreateSubdirectory("maps");
    foldercheck.CreateSubdirectory("sounds");
    foldercheck.CreateSubdirectory("music");
    foldercheck.CreateSubdirectory("character_sheets");
    foldercheck.CreateSubdirectory("models");
    Prj p = new Prj();
    p.name = filename;
    p.mapcount = 0; p.palettelen = 0; p.lastmap = 0;
    p.names = new string[1];
    p.names[0] = "unnamed";
    p.wm = true;
    control.dc.p = p;
    filename = "";
    loadlist.options.Clear();
    ClearPalette();
    paint = new Paint();
    paint.flagset = tflags;
    paint.mat = imat;
    paint.name = "Default";
    paint.color = tc;
    palette.Add(paint);
    BuildButton(paint);
    StartCoroutine(SavePaint(paint));
    StartCoroutine(SaveImage(control.dc.direc + paints + paint.name + ".png", (Texture2D)imat.mainTexture));
    currMap = 0;
    genmenu.SetActive(true);
    control.GetComponentInChildren<MapGenerator>(true).gameObject.SetActive(true);
  }
  public void LoadProject(){StartCoroutine(LoadPrj());}
  public IEnumerator LoadPrj(){
    yield return SaveDialog();
    ClearMap();
    control.dc.direc = control.dc.ddirec;
    loadprjmenu.SetActive(true);
    }
  public void SetFilename(){
    lprje.SetActive(false);
    string[] TSA = StandaloneFileBrowser.OpenFilePanel("Select Project File...",control.dc.ddirec,"prj",false);
    try{if(TSA[0]!=null){filename = TSA[0]; loadpath.text = TSA[0];}}
    catch(System.IndexOutOfRangeException){}
    }
  //anything that references filename needs to be cleared afterwards
  public void LoadAProject(){StartCoroutine(LoadPrj(filename));
  filename = "";}
  public IEnumerator LoadPrj(string path){
    FileInfo fi = new FileInfo(path);
    if(fi.Exists){FileStream file = fi.Open(FileMode.Open, FileAccess.ReadWrite);
    ValueTask<Prj> t = JsonSerializer.DeserializeAsync<Prj>(file);
    yield return new WaitUntil(() => t.IsCompleted);
    control.dc.p = t.Result;
    loadprjmenu.SetActive(false);
    file.Close();
    tprj = control.dc.p;
    control.dc.direc = path.Substring(0, path.Length - (control.dc.p.name.Length + 4));
    //print(control.dc.direc);
    List<String> l = new List<String>(control.dc.p.names.Length);
    for(int i = 0; i < control.dc.p.names.Length; i++){l.Add(control.dc.p.names[i]);}
    loadlist.options.Clear();
    loadlist.AddOptions(l);
    ClearPalette();
    yield return LoadPalette();
    loadmapmenu.SetActive(true);}
    else{lprje.SetActive(true);}
    yield break;
    }
  public void SetProject(Prj p){
    control.dc.p = p;
    List<string> names =  new List<string>();
    for(int i = 0; i < control.dc.p.mapcount; i++){names.Add(control.dc.p.names[i]);}
    loadlist.AddOptions(names);
    currMap = control.dc.p.lastmap;
    LoadMap(currMap);
  }
  public IEnumerator SavePrj(){
    FileStream file = new FileStream(control.dc.direc + control.dc.p.name + ".prj",FileMode.Create,FileAccess.ReadWrite,FileShare.ReadWrite);
    Task t = JsonSerializer.SerializeAsync<Prj>(file,control.dc.p);
    yield return new WaitUntil(() => t.IsCompleted);
    file.Close();
    yield break;
  }
  public void UpdateProject(){control.dc.p = tprj;}
  #endregion
  #region resourcemanage
  //fileaccess being async ensures responsiveness from the interface
  public IEnumerator<Texture2D> LoadImage(string path){
    Texture2D i = new Texture2D(2,2);
    FileInfo fi =  new FileInfo(path);
    FileStream file = new FileStream(path, FileMode.Open,FileAccess.Read,FileShare.Read);
    byte[] bits = new byte[fi.Length];
    Task<int> t = file.ReadAsync(bits,0,(int)fi.Length);
    while(!t.IsCompleted){yield return null;}
    i.LoadImage(bits);
    file.Close();
    yield return i;
  }
  public IEnumerator SaveImage(string path, Texture2D t){
    FileStream file = new FileStream(path,FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
    byte[] bits = t.EncodeToPNG();
    Task j = file.WriteAsync(bits,0,bits.Length);
    yield return new WaitUntil(() => j.IsCompleted);
    file.Close();
    yield break;
  }
  #endregion
}
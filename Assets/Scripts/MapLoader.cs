using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using System;
using HMTac;

public class MapLoader : MonoBehaviour{ 
  #region datastore
  public GameObject hextile, loadmenu;
  public Paint d, temp;
  public Material bmat;
  static float sin60 = Mathf.Sin(Mathf.Deg2Rad * 60);
  public float xShift, xPos, zShift, zPos;
  float maxheight;
  MapController m;
  public EditorControl e;
  #endregion
  //load in the editor
  public IEnumerator ELoad(string path) {
    loadmenu.SetActive(false);
    m = GetComponentInParent<MapController>();
    FileStream file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite); 
    ValueTask<HMp> t = JsonSerializer.DeserializeAsync<HMp>(file);
    yield return new WaitUntil(() => t.IsCompleted);
    m.map = t.Result;
    file.Close();
    //zLen gets recalculated instead of being stored in menu
    //tiny memory savings adds up
    int zLen = m.map.grid.Length / m.map.xLen;
    m.extras = new List<GameObject>();
    m.grid = new GameObject[m.map.xLen,zLen];
    //this is for game loading, which will load a pseudo-dictionary to save memory
    //designing for toasters
    m.palette.Clear();
    //fallback in case of failure
    d = new Paint();
    d.mat = bmat;
    d.index = -1;
    d.flagset = new BitArray(8,false);
    d.color = new Vector4(1,1,1,1);
    d.name = "default";
    xPos = 0;
    xShift = m.scale * 1.5f;
    m.lrShift = xShift;
    zPos = 0;
    zShift = (m.scale * sin60 * 2f);
    m.fbShift = zShift;
    for (int j = 0; j < m.map.xLen; j++) {
      zPos = (j % 2 == 0) ? 0 : m.scale * sin60;
      for (int k = 0; k < zLen; k++) {
        if (m.map.grid[j,k] == null) BlankTile((ushort)j,(ushort)k);
        else {
          NewTileE(
            (ushort)j,
            m.map.grid[j,k].yVal,
            (ushort)k,
            m.map.grid[j,k].rot,
            m.map.grid[j,k].paintindex,
            true);
        }
      zPos += zShift;
      }
    xPos += xShift;
    }
    m.xSel = m.map.xLen / 2; m.zSel = zLen / 2;
    m.FinishELoad();
    e.UpdateBar();
    e.maxHeight = maxheight;
    e.topbar.SetActive(true);
    yield break;
  }
  //load in engine
  public IEnumerator GLoad(string path) {
    m = GetComponentInParent<MapController>();
    FileStream file = new FileStream(path, FileMode.Open);
    ValueTask<HMp> t = JsonSerializer.DeserializeAsync<HMp>(file);
    yield return new WaitUntil(() => t.IsCompleted);
    m.map = t.Result;
    file.Close();
    int zLen = m.map.grid.Length / m.map.xLen;
    m.grid =  new GameObject[m.map.xLen,zLen];
    m.palette.Clear();
    for (int j = 0; j < m.map.xLen; j++) {
      for (int k = 0; k < m.map.grid.Length/m.map.xLen;k++) {
        if (m.map.grid[j,k] != null) {
          try {
            NewTileG(
              (ushort)j,
              m.map.grid[j,k].yVal,
              (ushort)k,
              m.map.grid[j,k].rot,
              m.map.grid[j,k].paintindex,
              true);
          }
        catch(Exception e){Console.Write(e);}}
      }
    } 
    yield break;
  }
  public void NewTileE(ushort x, ushort y, ushort z, byte r, int p, bool a) {
    MapTileObject hex;
    GameObject cell;
    cell = Instantiate<GameObject>(
      hextile, 
      new Vector3(xPos, y * (m.scale / 2), zPos), 
      Quaternion.Euler(-90,0,r*60), 
      m.transform);
    cell.transform.localScale = m.scalevec;
    maxheight = Mathf.Max(maxheight, cell.transform.position.y);
    m.grid[x,z] = cell;
    hex = cell.GetComponent<MapTileObject>();
    try {temp = e.palette[p];}
    catch(System.IndexOutOfRangeException) {temp = d;}
    hex.Instantiate(x,y,z,r,temp,a);
  }
  public void NewTileG(ushort x, ushort y, ushort z, byte r, int p, bool a) {
    MapTileObject hex;
    GameObject cell;
    cell = Instantiate<GameObject>(
      hextile,
      new Vector3(xPos, y * (m.scale / 2), zPos), 
      Quaternion.Euler(-90,0,r*60), 
      m.transform);
    cell.transform.localScale = m.scalevec;
    maxheight = Mathf.Max(maxheight, cell.transform.position.y);
    m.grid[x,z] = cell;
    hex = cell.GetComponent<MapTileObject>();
    //search mapcontroller palette for relevant paint, otherwise load
    hex.Instantiate(x,y,z,r,temp,a);
  }
  public void BlankTile(ushort x, ushort z) {
    MapTileObject hex;
    GameObject cell;
    cell = Instantiate<GameObject>(
      hextile, 
      new Vector3(xPos,0, zPos), 
      Quaternion.Euler(-90,0,0), m.transform);
    cell.transform.localScale = m.scalevec;
    m.grid[x,z] = cell;
    hex = cell.GetComponent<MapTileObject>();
    hex.Instantiate(x,0,z,0,d,false);
    m.Remove(cell);
  }
  //exists for engine, not currently used
  public IEnumerator LoadPaint(int index) {
    FileStream file = new FileStream(m.dc.direc + "paints/" + index + ".pnt", FileMode.Open, FileAccess.Read, FileShare.Read);
    ValueTask<Paint> tp = JsonSerializer.DeserializeAsync<Paint>(file);
    yield return new WaitUntil(() => tp.IsCompleted);
    temp = tp.Result;
    file.Close();
    FileInfo fi = new FileInfo(m.dc.direc + "paints/" + temp.name + ".png");
    file = new FileStream(m.dc.direc + "paints/" + temp.name + ".png", FileMode.Open, FileAccess.Read, FileShare.Read);
    byte[] bits = new byte[fi.Length];
    Task<int> tt = file.ReadAsync(bits,0,(int)fi.Length);
    yield return new WaitUntil(() => tt.IsCompleted);
    file.Close();
    Texture2D tex = new Texture2D(60,119);
    tex.filterMode = FilterMode.Point;
    tex.LoadImage(bits);
    temp.mat = new Material(bmat);
    temp.mat.mainTexture = tex;
    temp.mat.color = temp.color;
    m.palette.Add(new KeyValuePair<int, Paint>(index, temp));
    yield break;}
}

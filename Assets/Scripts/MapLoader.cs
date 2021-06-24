using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using System;
using HMTac;

public class MapLoader : MonoBehaviour
{
  public GameObject hextile, loadmenu;
  public paint d, temp;
  public Material bmat;
  static float sin60 = Mathf.Sin(Mathf.Deg2Rad * 60);
  MapController m;
  public EditorControl e;
  //load in the editor
  public IEnumerator eLoad(string path){loadmenu.SetActive(false);
    m = GetComponentInParent<MapController>();
    FileStream file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite); 
    ValueTask<hmp> t = JsonSerializer.DeserializeAsync<hmp>(file);
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
    d = new paint();
    d.mat = bmat;
    d.index = -1;
    d.flagset = new BitArray(8,false);
    d.color = new Vector4(1,1,1,1);
    d.name = "default";
    for(int j = 0; j < m.map.xLen; j++){
      for(int k = 0; k < zLen; k++){
        if(m.map.grid[j,k] == null){blankTile((ushort)j,(ushort)k);}
        else{newTileE((ushort)j,m.map.grid[j,k].yVal,(ushort)k,m.map.grid[j,k].rot,m.map.grid[j,k].paintindex,true);}
      }}m.xSel = m.map.xLen / 2; m.zSel = zLen / 2;
    m.finishELoad();
    e.updateBar();
    e.topbar.SetActive(true);
    yield break;
  }
  //load in engine
  public IEnumerator gLoad(string path){m = GetComponentInParent<MapController>();
    FileStream file = new FileStream(path, FileMode.Open);
    ValueTask<hmp> t = JsonSerializer.DeserializeAsync<hmp>(file);
    yield return new WaitUntil(() => t.IsCompleted);
    m.map = t.Result;
    file.Close();
    int zLen = m.map.grid.Length / m.map.xLen;
    m.grid =  new GameObject[m.map.xLen,zLen];
    m.palette.Clear();
    for(int j = 0; j < m.map.xLen; j++){
      for(int k = 0; k < m.map.grid.Length/m.map.xLen;k++){
        if(m.map.grid[j,k] != null){try{newTileE((ushort)j,m.map.grid[j,k].yVal,(ushort)k,m.map.grid[j,k].rot,m.map.grid[j,k].paintindex,true);}
        catch(Exception e){Console.Write(e);}}
      }} yield break;
  }
  public void newTileE(ushort x, ushort y, ushort z, byte r, int p, bool a){
    MapTileObject hex;
    GameObject cell;
    cell = Instantiate<GameObject>(hextile, new Vector3((x *  m.scale * 1.5f), y * (m.scale / 2), z * m.scale * sin60 * 2 + ((x % 2 == 0) ? 0 : m.scale * sin60)),Quaternion.Euler(-90,0,r*60),m.transform);
    cell.transform.localScale = m.scalevec;
    m.grid[x,z] = cell;
    cell.AddComponent<MapTileObject>();
    hex = cell.GetComponent<MapTileObject>();
    paint paint;
    try{paint = e.palette[p];}
    catch(System.IndexOutOfRangeException){paint = d;}
    hex.Instantiate(x,y,z,r,paint,a);
  }
  public void blankTile(ushort x, ushort z){
    MapTileObject hex;
    GameObject cell;
    cell = Instantiate<GameObject>(hextile, new Vector3((x *  m.scale * 1.5f),0, z * m.scale * sin60 * 2 + ((x % 2 == 0) ? 0 : m.scale * sin60)),Quaternion.Euler(-90,0,0),m.transform);
    cell.transform.localScale = m.scalevec;
    cell.AddComponent<MapTileObject>();
    m.grid[x,z] = cell;
    hex = cell.GetComponent<MapTileObject>();
    hex.Instantiate(x,0,z,0,d,false);
    m.remove(cell);
  }
  //exists for engine, not currently used
  public IEnumerator loadPaint(int index){FileStream file = new FileStream(m.dc.direc + "paints/" + index + ".pnt", FileMode.Open, FileAccess.Read, FileShare.Read);
    ValueTask<paint> tp = JsonSerializer.DeserializeAsync<paint>(file);
    yield return new WaitUntil(() => tp.IsCompleted);
    paint p = tp.Result;
    file.Close();
    FileInfo fi = new FileInfo(m.dc.direc + "paints/" + p.name + ".png");
    file = new FileStream(m.dc.direc + "paints/" + p.name + ".png", FileMode.Open, FileAccess.Read, FileShare.Read);
    byte[] bits = new byte[fi.Length];
    Task<int> tt = file.ReadAsync(bits,0,(int)fi.Length);
    yield return new WaitUntil(() => tt.IsCompleted);
    file.Close();
    Texture2D tex = new Texture2D(60,119);
    tex.filterMode = FilterMode.Point;
    tex.LoadImage(bits);
    p.mat = new Material(bmat);
    p.mat.mainTexture = tex;
    p.mat.color = p.color;
    temp = p; yield break;}
}

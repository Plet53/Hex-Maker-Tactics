using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HMTac;

public class MapGenerator : MonoBehaviour
{
  public ushort xLen, zLen;
  //text input
  public void setXLen(string x){xLen = (ushort)int.Parse(x);}
  public void setXLen(ushort x){xLen = x;}
  public void setZLen(string z){zLen = (ushort)int.Parse(z);}
  public void setZLen(ushort z){zLen = z;}
  //turns out! sin 60 degrees comes up a lot when dealing with hexagons
  static float sin60 = Mathf.Sin(Mathf.Deg2Rad * 60);
  public GameObject hexTile, genMenu, editor;
  public EditorControl e;
  public IEnumerator Generate(int p){genMenu.SetActive(false);
    //instantiate pile
    MapController parent = GetComponentInParent<MapController>();
    parent.dc.p.mapcount++;
    parent.grid = new GameObject[xLen,zLen];
    parent.map.grid = new cell[xLen,zLen];
    parent.extras = new List<GameObject>();
    parent.map.xLen = xLen;
    //shift values used to prevent constant re-evaluation
    float xPos = 0;
    float xShift = parent.scale * (float)1.5; parent.lrShift = xShift;
    float zPos = 0;
    float zShift = (parent.scale * sin60 * (float)2); parent.fbShift = zShift;
    MapTileObject hex;
    GameObject cell;
    for (ushort j = 0; j < xLen; j++){
      //ternaries <3
      zPos = (j % 2 == 1) ? parent.scale * sin60: 0;
      for (ushort k = 0; k < zLen; k++){
        cell = Instantiate<GameObject>(hexTile, new Vector3(xPos, 0, zPos), Quaternion.AngleAxis(90, Vector3.left), parent.transform);
        cell.GetComponent<Transform>().localScale = parent.scalevec;
        hex = cell.AddComponent<MapTileObject>();
        parent.grid[j,k] = cell;
        //look there are a lot of properties to deal with here
        hex.Instantiate(j,0,k,0,e.palette[p],true);
        zPos += zShift;
        parent.map.grid[j,k] = hex.toCell();}
      xPos += xShift;
    }
    parent.xSel = xLen / 2;
    parent.zSel = zLen / 2;
    e.addtoloadmenu(e.currMap);
    parent.finishGen();
    e.updateBar();
    //topbar part of the interface isn't active when touching it would cause problems
    e.topbar.SetActive(true);
    yield break;
  }
  //regen does rectangle in 3 parts
  public IEnumerator ReGenerate(int p, ushort x, ushort z){cell[,] c = new cell[x,z];
    MapController parent = GetComponentInParent<MapController>();
    GameObject[,] grid = new GameObject[x,z];
    MapTileObject hex;
    GameObject cell;
    float xShift = parent.scale * (float)1.5;
    float zShift = (parent.scale * sin60 * (float)2);
    float xPos = 0;
    float zPos = 0;
    zLen = (ushort)(parent.grid.Length / parent.map.xLen);
    ushort j = 0;
    ushort k = 0;
    //first, the bottom left segment
    while(j < parent.map.xLen){
      while(k < zLen){if((j >= x) || (k >= z)){Destroy(parent.grid[j,k]);}
        else{grid[j,k] = parent.grid[j,k]; c[j,k] = grid[j,k].GetComponent<MapTileObject>().toCell();}
        k++;}
      k = 0; j++;}
    //then, the right wall
    if(x > parent.map.xLen){xPos = xShift * parent.map.xLen;
    while(j < x){zPos = (j % 2 == 1) ? parent.scale * sin60: 0;
      while(k < z){
        cell = Instantiate<GameObject>(hexTile, new Vector3(xPos, 0, zPos), Quaternion.AngleAxis(90, Vector3.left), parent.transform);
        cell.GetComponent<Transform>().localScale  = parent.scalevec;
        hex = cell.AddComponent<MapTileObject>();
        grid[j,k] = cell;
        hex.Instantiate(j,0,k,0,e.palette[p],true);
        c[j,k] = hex.toCell();
      zPos += zShift; k++;}
    xPos += xShift; k = 0; j++;}}
    //finally, the segment above the inital box, and to the left of the wall
    if(z > zLen){xPos = 0;
      j = 0;
      k = zLen;
      while((j < x) && (j < parent.map.xLen)){zPos = k * zShift + ((j % 2 == 1) ? parent.scale * sin60 : 0);
        while(k < z){
          cell = Instantiate<GameObject>(hexTile, new Vector3(xPos, 0, zPos), Quaternion.AngleAxis(90, Vector3.left), parent.transform);
          cell.GetComponent<Transform>().localScale  = parent.scalevec;
          hex = cell.AddComponent<MapTileObject>();
          grid[j,k] = cell;
          hex.Instantiate(j,0,k,0,e.palette[p],true);
          c[j,k] = hex.toCell();
          zPos += zShift; k++;}
        xPos += xShift; k = zLen; j++;}}
    parent.map.xLen = x;
    parent.map.grid = c;
    parent.grid = grid;
    parent.xSel = x / 2;
    parent.zSel = z / 2;
    parent.finishGen();
    yield break;
  }
}

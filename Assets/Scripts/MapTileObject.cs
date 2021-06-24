using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using HMTac;

public class MapTileObject : MonoBehaviour
{
  public ushort xVal, yVal, zVal;
  public byte rot;
  public bool act;
  //TODO: feels wasteful as of now, opinion might change further down the line
  public List<KeyValuePair<ushort, GameObject>> adjacencyTable;
  public paint p;
  public void Instantiate(ushort x, ushort y, ushort z, byte r, paint paint, bool a){xVal = x; yVal = y; zVal = z; rot = r; act = a; this.applyPaint(paint);} 
  void setAdjacent(int x, int z, ushort dir)
  {
    try {adjacencyTable.Add(new KeyValuePair<ushort, GameObject>(dir, this.GetComponentInParent<MapController>().grid[x,z]));}
    catch (System.IndexOutOfRangeException){}
    foreach(GameObject e in this.GetComponentInParent<MapController>().extras)
      {if((e.GetComponent<MapTileObject>().xVal == x) && (e.GetComponent<MapTileObject>().zVal == z)){
        adjacencyTable.Add(new KeyValuePair<ushort, GameObject>(dir, e));}}
  }
  public cell toCell(){if(act){cell c = new cell();
  c.yVal = yVal;
  c.rot = rot;
  c.paintindex = p.index;
  return c;}
  else{return null;}}
    //TODO: change to Vector2int
  public void setupAdjacencyTable(){adjacencyTable = new List<KeyValuePair<ushort, GameObject>>();
    if(xVal % 2 == 0){
      setAdjacent(xVal,zVal + 1, 0); //north
      setAdjacent(xVal + 1,zVal, 1); //northeast
      setAdjacent(xVal + 1,zVal - 1, 2); //southeast
      setAdjacent(xVal,zVal - 1, 3); //south
      setAdjacent(xVal - 1,zVal - 1, 4); //southwest
      setAdjacent(xVal - 1,zVal, 5); //northwest
      }
    else{
      setAdjacent(xVal,zVal + 1, 0); //north
      setAdjacent(xVal + 1,zVal + 1, 1); //northeast
      setAdjacent(xVal + 1,zVal, 2); //southeast
      setAdjacent(xVal,zVal - 1, 3); //south
      setAdjacent(xVal - 1,zVal, 4); //southwest
      setAdjacent(xVal - 1,zVal + 1, 5); //northwest
      }
  }
  public List<GameObject> adjacent(ushort dir){List<GameObject> ret = new List<GameObject>();
    Lookup<ushort,GameObject> lu = (Lookup<ushort,GameObject>)adjacencyTable.ToLookup(e => e.Key, e => e.Value);
    foreach(GameObject e in lu[dir]){ret.Add(e);}
    return ret;}
  //Using a box collider and the physics engine to deal with rectangle select
  public void OnTriggerEnter(){GetComponentInParent<MapController>().tempSelect(this.gameObject);}
  public void OnTriggerExit(){if(GetComponentInParent<MapController>().tempSelectList[0] != null){GetComponentInParent<MapController>().tempDeselect(this.gameObject);}}
  public void applyPaint(paint paint){p = paint; this.gameObject.GetComponent<Renderer>().material = paint.mat;}
  public void setHeight(ushort h){yVal = h;}
  //values are unsigned
  public void changeHeight(int h){yVal = (ushort)Mathf.Max(0, yVal + h);}
  //hey why doesn't ushort have support for modular arithmetic
  public void rotLeft(){rot = (byte)(rot + 5);
  this.gameObject.GetComponent<Transform>().Rotate(new Vector3(0,0,300));}
  public void rotRight(){rot = (byte)(rot + 1);
  this.gameObject.GetComponent<Transform>().Rotate(new Vector3(0,0,60));}
  public void setRot(ushort d){rot = (byte)d;
    this.gameObject.transform.rotation = Quaternion.Euler(0,this.gameObject.transform.rotation.eulerAngles.y, 60 * rot);}
  void Update(){}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//TODO: implement
//the fundamental premise: a camera which responds to points on a given map being selected, with static rotations.
public class GameCamera : MonoBehaviour{
  #region datastore
  public GameObject controlPoint, cam;
  private GameObject[] points;
  public Vector3[] fbdirections, lrdirections;
  public int angle, zoom, xFoc, zFoc;
  public byte currCam;
  public bool u;
  #endregion
  void Start() {
    //Position of the camera when pointing in any given direction
    points = new GameObject[6];
    //Distance and direction in which camera shifts
    fbdirections = new Vector3[6];
    lrdirections = new Vector3[6];
    //TODO: orthographic camera uses size to do zoom, fix
    float yPos = this.transform.position.y - (float)(zoom / 4);
    Vector3 pos;
    int angle = 0;
    float xAngle = 0;
    float zAngle = 0;
    //Initialize the structures above
    for (int i = 0; i < 6; i++) {
      xAngle = Mathf.Sin(Mathf.Deg2Rad * angle);
      zAngle = Mathf.Cos(Mathf.Deg2Rad * angle);
      fbdirections[i] = new Vector3(xAngle, 0, zAngle);
      pos = new Vector3(((zoom * xAngle) + this.transform.position.x), yPos, ((zoom * zAngle) + this.transform.position.z));
      points[i] = Instantiate<GameObject>(controlPoint, pos, Quaternion.Euler(30, (angle + 180), 0), this.transform);
      angle += 30;
      xAngle = Mathf.Sin(Mathf.Deg2Rad * angle);
      zAngle = Mathf.Cos(Mathf.Deg2Rad * angle);
      lrdirections[i] = new Vector3(xAngle, 0, zAngle);
      angle += 30;
    }
    cam.transform.position = points[3].transform.position;
    cam.transform.rotation = points[3].transform.rotation;
  }
  //Camera controls
  void Update() {
    if (Input.GetAxis("Horizontal") < 0) {
      currCam = (byte)((currCam + 1) % 6);
      if (u) cam.transform.rotation = Quaternion.Euler(90, points[currCam].transform.rotation.eulerAngles.y,0);
      else {
        cam.transform.position = points[currCam].transform.position;
        cam.transform.rotation = points[currCam].transform.rotation;
      }
    }
    if (Input.GetAxis("Horizontal") > 0) {
      currCam = (byte)((currCam + 5) % 6);
      if (u) cam.transform.rotation = Quaternion.Euler(90, points[currCam].transform.rotation.eulerAngles.y,0);
      else { 
        cam.transform.position = points[currCam].transform.position;
        cam.transform.rotation = points[currCam].transform.rotation;
      }
    }
    if(Input.GetAxis("Vertical") < 0) {
      u = false;
      cam.transform.position = points[currCam].transform.position;
      cam.transform.rotation = points[currCam].transform.rotation;
    }
    if(Input.GetAxis("Vertical") > 0) {
      u = true;
      cam.transform.position = this.gameObject.transform.position;
      cam.transform.rotation = Quaternion.Euler(90, cam.transform.rotation.eulerAngles.y ,0);
    }
  }
}

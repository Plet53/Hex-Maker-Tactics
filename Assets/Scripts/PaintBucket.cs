using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PaintBucket : MonoBehaviour
{ public int index;
  public EditorControl e;
  public TextMeshProUGUI t;
  public GameObject[] flags;
  public Texture2D preview, copy;
  public RawImage image;
  //instantiated in unity editor, essentially ints that point to the bottom left corner of the preview image
  public Vector2Int[] frames;
  public void setFrame(int i)
    {Graphics.CopyTexture(preview,0,0,frames[i].x,frames[i].y,40,36,copy,0,0,0,0);}
  public void bepis(){e.setPaint(index);}
  //TODO: variable flag count for individual projects
  //always % 8 though, fullbytes only
  public void dispflags(){for(int i = 0; i < e.palette[index].flagset.Length; i++){flags[i].SetActive(e.palette[index].flagset[i]);}}
}

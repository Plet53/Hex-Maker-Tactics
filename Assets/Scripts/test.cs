using System.Collections;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class test : MonoBehaviour
{
  public Camera previewCam;
	public Texture2D pre;
	public RenderTexture preview, presave;
	public RawImage image;


  IEnumerator Start()
  {
		yield return new WaitForEndOfFrame();
		pre = new Texture2D(120,72, TextureFormat.RGBAFloat, true, true);
		pre.filterMode = 0;
    StartCoroutine(buildPreview());
  }
	public IEnumerator buildPreview(){Vector3 sixths = new Vector3(0,60,0);
		StartCoroutine(saveImage("../preview0.png",pre));
		for(int j = 0; j < 2; j++){
      for(int k = 0; k < 3; k++){
        previewCam.Render();
        Graphics.CopyTexture(preview,0,0,0,0,40,36,presave,0,0,(40*k),(36*j));
        pre.IncrementUpdateCount();
        this.gameObject.GetComponent<Transform>().Rotate(sixths, Space.World);}}
		image.texture = presave;
		Graphics.SetRenderTarget(presave);
		pre.ReadPixels(new Rect(0,0,120,72),0,0,true);
		pre.Apply();
		StartCoroutine(saveImage("../preview1.png",pre));
		yield break;
	}
	public IEnumerator saveImage(string path, Texture2D t){FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write);
    byte[] bits = t.EncodeToPNG();
    print(bits.Length);
    Task j = file.WriteAsync(bits,0,bits.Length);
    yield return new WaitUntil(() => j.IsCompleted);
    yield break;}
}

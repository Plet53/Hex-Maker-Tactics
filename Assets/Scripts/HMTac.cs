using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HMTac {
  [JsonConverter(typeof(PaintJSONConverter))]
  public class Paint {
     //stored here to return to after given effects, such as selection display or spells in game
    public Material mat;
    //flags have no inherent purpose, use as you see fit
    public BitArray flagset;
    //stored for serialization
    public Vector4 color;
    //paints are stored elsewhere as an index
    public int index;
    public string name;
    public override string ToString() {
      return "name: " + name +
             "material: " + mat.ToString() +
             "\nflags: " + FlagsToString();
    }
    //helpers for serialization
    public string FlagsToString() {
      char[] r = new char[flagset.Length];
      for (int i = 0; i < flagset.Length; i++) r[i] = flagset[i] ? '1' : '0';
      return new string(r);
    }
    public void StringToFlags(string s) {
      flagset = new BitArray(s.Length, false);
      char[] c = s.ToCharArray();
      for (int i = 0; i < flagset.Length; i++) {
        try{flagset[i] = (c[i] == '1') ? true : false;}
        catch(System.IndexOutOfRangeException){}
      }
    }
  }
  //does not have a converter, this object only exists for the purposes of saving information from the main grid
  public class Cell {
    public ushort yVal;
    byte r;
    public byte rot {get {return r;} set {r = (byte)((int)value % 6);}}
    public int paintindex;
  }
  [JsonConverter(typeof(ProjectJSONConverter))]
  public struct Prj {
    public string name;
    //names are here for things being referenced elsewhere
    public string[] names;
    public int mapcount, palettelen, lastmap;
    //stores whether or not a given project is designed around maps being selected from a larger map
    public bool wm;
  }
  [JsonConverter(typeof(HexMapJSONConverter))]
  public struct HMp {
    public Cell[,] grid;
    //nullable to allow for non-rectangular maps, should save memory
    public ushort xLen;
    //don't need to store zLen dyring runtime, can be derived
    //public List<GameObject> extras;
    //TODO: gotta work this out at some point
  }
  [JsonConverter(typeof(OptionsJsonConverter))]
  //sound isn't implemented yet, this is here for that
  public class Options {
    //master, music, sound effects, ambient tracks (bird chirp, rain, etc.)
    float m,mu,fx,a;
    //fullscreen
    public bool f;
    public float mVol{get {return m;} set {m = Mathf.Clamp(value, 0, 2f);}}
    public float muVol{get {return mu;} set {mu = Mathf.Clamp(value, 0, 2f);}}
    public float fxVol{get {return fx;} set {fx = Mathf.Clamp(value, 0, 2f);}}
    public float aVol{get {return a;} set {a = Mathf.Clamp(value, 0, 2f);}}
    public void setVols(float[] f) {m  = f[0]; fx = f[1]; mu = f[2]; a = f[3];}
    public void setVols() {m = 1f; fx = 1f; mu = 1f; a = 1f;}}
  //I accept that the way I have implemented these is, perhaps, inelegant
  //But it works
  //Maybe the default json writer/reader's methods should skip over startobject/array, howboutthat
  public class PaintJSONConverter : JsonConverter<Paint> {
    public override Paint Read(ref Utf8JsonReader r, Type T, JsonSerializerOptions j) {
      Paint p = new Paint();
      p.mat = null; //re-instantiate materials at runtime
      r.Read(); r.Read();
      p.name = r.GetString();
      r.Read(); r.Read();
      p.StringToFlags(r.GetString());
      Vector4 c = Vector4.zero;
      r.Read(); r.Read(); r.Read();
      c.x = r.GetSingle();
      r.Read();
      c.y = r.GetSingle();
      r.Read();
      c.z = r.GetSingle();
      r.Read();
      c.w = r.GetSingle();
      p.color = c;
      r.Read(); r.Read(); r.Read();
      p.index = r.GetInt32();
      r.Read(); r.Read();
      return p;
    }
    public override void Write(Utf8JsonWriter w, Paint p, JsonSerializerOptions j) {
      w.WriteStartObject();
      w.WriteString("name",p.name);
      w.WriteString("flags",p.FlagsToString());
      w.WriteStartArray("color RGBA");
      w.WriteNumberValue(p.color.x);
      w.WriteNumberValue(p.color.y);
      w.WriteNumberValue(p.color.z);
      w.WriteNumberValue(p.color.w);
      w.WriteEndArray();
      w.WriteNumber("Index",p.index);
      w.WriteEndObject();
    }
  }
  public class ProjectJSONConverter : JsonConverter<Prj> {
    public override Prj Read(ref Utf8JsonReader r, Type T, JsonSerializerOptions j) {
      Prj p = new Prj();
      r.Read(); r.Read();
      p.name = r.GetString();
      r.Read(); r.Read();
      p.mapcount = r.GetInt32();
      //I still have to instantiate this
      p.names = new string[Mathf.Max(p.mapcount,1)];
      r.Read(); r.Read(); r.Read();
      for(int i = 0; i < p.mapcount; i++){
        p.names[i] = r.GetString(); r.Read();}
      r.Read(); r.Read();
      p.palettelen = r.GetInt32();
      r.Read(); r.Read();
      p.lastmap = r.GetInt32();
      r.Read(); r.Read();
      p.wm = r.GetBoolean();
      r.Read(); r.Read();
      return p;
    }
    public override void Write(Utf8JsonWriter w, Prj p, JsonSerializerOptions j) {
      w.WriteStartObject();
      w.WriteString("name",p.name);
      w.WriteNumber("map count",p.mapcount);
      w.WriteStartArray("map names");
      for (int i = 0; i < p.names.Length; i++) w.WriteStringValue(p.names[i]);
      w.WriteEndArray();
      w.WriteNumber("paint count",p.palettelen);
      w.WriteNumber("last map",p.lastmap);
      w.WriteBoolean("world map?", p.wm);
      w.WriteEndObject();
    }
  }
  public class HexMapJSONConverter : JsonConverter<HMp> {
    public override bool HandleNull => true;
    public override HMp Read(ref Utf8JsonReader r, Type T, JsonSerializerOptions j) {
      HMp h = new HMp();
      r.Read(); r.Read();
      ushort a = r.GetUInt16();
      r.Read(); r.Read();
      ushort b = r.GetUInt16();
      h.xLen = a;
      h.grid = new Cell[a,b];
      r.Read(); r.Read(); r.Read();
      for(int c = 0; c < a; c++){
        r.Read();
          for(int d = 0; d < b; d++){
            r.Read(); r.Read();
            if(r.TokenType == JsonTokenType.Null){h.grid[c,d] = null;}
            else{
              Cell e = new Cell();
              e.yVal = r.GetUInt16();
              r.Read(); r.Read();
              e.rot = r.GetByte();
              r.Read(); r.Read();
              e.paintindex = r.GetInt32();
              h.grid[c,d] = e;}
          r.Read(); r.Read();}
        r.Read();}
      r.Read(); r.Read(); r.Read();
      /*TODO: an implementation for objects outside of the grid
      h.extras = new List<GameObject>();
      string jsonObject;
      while(r.TokenType != JsonTokenType.EndArray){
        jsonObject = r.GetString();
        if(jsonObject != null){h.extras.Add(JsonUtility.FromJson<GameObject>(jsonObject)); r.Read();
      }}
      r.Read(); r.Read();*/
      return h;
    }
    public override void Write(Utf8JsonWriter w, HMp h, JsonSerializerOptions j) {
      int len = h.grid.Length / h.xLen;
      w.WriteStartObject();
      w.WriteNumber("width",h.xLen);
      w.WriteNumber("length",len);
      w.WriteStartArray("grid");
      for(int a = 0; a < h.xLen; a++) {
        w.WriteStartArray();
        for(int b = 0; b < len; b++) {
          w.WriteStartObject();
          if (h.grid[a,b] == null) w.WriteNull("o");
          else {
            Cell c = (Cell)h.grid[a,b];
            w.WriteNumber("yVal",c.yVal);
            w.WriteNumber("Rot",c.rot);
            w.WriteNumber("Paint",c.paintindex);
          }
          w.WriteEndObject();
        }
        w.WriteEndArray();
      }
      w.WriteEndArray();
    /*TODO: an implementation for objects outside of the grid
    w.WriteStartArray("extras");
    foreach(GameObject e in h.extras){w.WriteStartObject();
      w.WriteStringValue(JsonUtility.ToJson(e));
      w.WriteEndObject();}
    w.WriteEndArray();*/
    w.WriteEndObject();
    }
  }
  public class OptionsJsonConverter : JsonConverter<Options> {
    public override Options Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions j) {
      Options o =  new Options();
      r.Read(); r.Read();
      o.f = r.GetBoolean();
      r.Read(); r.Read(); r.Read();
      o.mVol = r.GetSingle();
      r.Read();
      o.muVol = r.GetSingle();
      r.Read();
      o.fxVol = r.GetSingle();
      r.Read();
      o.aVol = r.GetSingle();
      r.Read(); r.Read();
      return o;
    }
    public override void Write(Utf8JsonWriter w, Options o, JsonSerializerOptions j) {
      w.WriteStartObject();
      w.WriteBoolean("fs",o.f);
      w.WriteStartArray("volumes");
      w.WriteNumberValue(o.mVol);
      w.WriteNumberValue(o.muVol);
      w.WriteNumberValue(o.fxVol);
      w.WriteNumberValue(o.aVol);
      w.WriteEndArray();
      w.WriteEndObject();
    }
  }
public class OutputCoroutine<T> { //look I'm going to need this a lot, might as well put it here
  public Coroutine c;
  public T r;
  private IEnumerator<T> t;
  public OutputCoroutine(MonoBehaviour o, IEnumerator<T> t) {
    this.t = t;
    this.c = o.StartCoroutine(Run());
  }
  private IEnumerator<T> Run() {while(t.MoveNext()){r = t.Current; yield return r;}}
  }
}
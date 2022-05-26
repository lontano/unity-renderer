using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DCL.Controllers;
using System.IO;

public class MapperCamera : MonoBehaviour
{
    [Header("Scene")]
    public Transform Target;
    public GameObject AvatarRenderer;
    public float ParcelSize = 16f;
    [Header("Render")]
    public Camera Camera;
    public RenderTexture RT;
    public Vector2Int RenderSize = new Vector2Int(1024, 1024);
    public string SessionNameBase = "Render_";
    public string RenderOutputPath = @"D:\Lukas\Renders\";
    public bool OverwriteFiles = true;
    [Header("Time outs")]
    public float ScreenShotTimeOut = 5f;
    public float GotoNextParcelTimeOut = 2f;
    [Header("Topology")]
    public int N = 1;
    public float flyingHeight = 10f;
    public bool SideCamera = false;
    public float OthographicCameraScale = 1.0f;
    public float SideCameraScale = 1.0f;
    private float distance = 50f;
    public Vector2Int InitialSquare = new Vector2Int(-150, -150);
    public Vector2Int FinalSquare = new Vector2Int(150, 150);

    public enum eParcelState
    {
        Idle,
        MovingToParcel,
        LoadingParcel,
        TakingSnapshot
    }
    public eParcelState ParcelState = eParcelState.Idle;

    private void Start()
    {
        //we don't want to be a child of the character controller because we want to be able to move by ouselfes
        //transform.parent = target;
        //to start up, just go over our character's position
        SetTransformPositoin(Target.position);        
        //If we didn't assign a camera manually, look for it on the current Gameobject
        if (Camera == null) Camera = GetComponent<Camera>();
        if (Camera == null) Camera = Camera.main;
    }

    /// <summary>
    /// Makes this game object take its place and orientation respect of where we want to look at
    /// </summary>
    /// <param name="targetPosition"></param>
    private void SetTransformPositoin(Vector3 targetPosition)
    {
        Vector3 offset = SideCamera ? new Vector3(2, 1, 2) : new Vector3(0, 1, 0);
        this.transform.position = targetPosition + offset * this.flyingHeight;
        this.transform.LookAt(targetPosition);
    }

    public bool ShowResolution = false;
    public bool Started = false;


    void Update()
    {
        if (this.SideCamera)
        {
            GetComponent<Camera>().orthographicSize = N * ParcelSize * SideCameraScale / 2;
        }
        else
        {
            GetComponent<Camera>().orthographicSize = N * ParcelSize * OthographicCameraScale / 2;
        }

        if (Input.GetKeyDown(KeyCode.Space) && !Started)
        {
            StartProcess();
        }

        // print screen resolution if P is pressed
        if (Input.GetKeyDown(KeyCode.P))
        {
            ShowResolution = !ShowResolution;
        }
        if (ShowResolution)
            Debug.Log("Screen resolution: " + Screen.width + "x" + Screen.height);


        //transform.position = Target.position + new Vector3(0, 1, 0) * distance;
        //transform.LookAt(Target);
        SetTransformPositoin(Target.position);
    }

    public void StartProcess () {
        Target.position += new Vector3(ParcelSize, 0, ParcelSize);
        GameObject debugView = GameObject.Find("DebugView(Clone)");
        if (debugView != null) debugView.SetActive(false);
        if (AvatarRenderer != null) AvatarRenderer.SetActive(false);
        Started = true;
        // go to first position
        // DCLCharacterController.i.SetPosition(new Vector3(parcelSize, 0, parcelSize));
        WaitForScreenshot();
    }

    public void StopProcess ()
    {
        Started = false;
    }

    public int currentX = 0, currentY = 0;
    public Vector3 currentPosition;
    public int layer = 1;
    public int leg = 0;
    private void GoToNextParcel()
    {
        // calculate next position
        // spiral around (0,0) through the grid
        // layer 0 -> (0,0)
        // layer 1 -> (1,0), (1,1), (0,1), (-1,1), (-1,0), (-1,-1), (0,-1), (1,-1)
        // layer 2 -> (2,0), (2,1), (2,2), (1,2), (0,2), (-1,2), (-2,2), (-2,1), (-2,0), (-2,-1), (-2,-2), (-1,-2), (0,-2), (1,-2), (2,-2), (2,-1)
        // etc...
        switch (leg)
        {
            case 0: ++currentX; if (currentX == layer) ++leg; break;
            case 1: ++currentY; if (currentY == layer) ++leg; break;
            case 2: --currentX; if (-currentX == layer) ++leg; break;
            case 3: --currentY; if (-currentY == layer) { leg = 0; ++layer; } break;
        }
        GoToParcel(currentX, currentY);

        if (this.Started)
        {
            waitStartTime = Time.time;
            Invoke("WaitForScreenshot", this.ScreenShotTimeOut);
        }
    }

    public void GoToParcel (int x, int y)
    {
        this.ParcelState = eParcelState.MovingToParcel;
        currentX = x;
        currentY = y;
        currentPosition = new Vector3(currentX * N * ParcelSize, flyingHeight, currentY * N * ParcelSize);

        if (Mathf.Abs(currentX * N) > 150f)
        {
            Debug.Log("Reached end of the world at " + currentPosition + ", layer " + layer);
            Started = false;
            Application.Quit();

            return;
        }

        string fullScreenshotPath = GetCurrentScreenshotPath();
        // check if screenshot was already taken and skip if so
        if (System.IO.File.Exists(fullScreenshotPath))
        {
            Debug.Log("Screenshot already exists for coordinate (" + currentX * N + "," + currentY * N + "), skipping...");
            Invoke("GoToNextParcel", 0.01f);
            return;
        }

        Debug.Log("now moving to position: (" + currentX * N + ", " + currentY * N + ")");
        // move player to current position
        Vector3 targetPosition = new Vector3(ParcelSize/2, 0, ParcelSize/2) + currentPosition;
        Vector3 delta = DCLCharacterController.i.characterPosition.worldPosition - targetPosition;

        if (delta.magnitude > 0.1f)
        {
            DCLCharacterController.i.SetPosition(targetPosition);
        }

        //we want to look at parcel's position
        this.SetTransformPositoin(targetPosition);
    }

    private float waitStartTime = 0f;
    public float waitTimeout = 60f;
    public float waitBeforeScreenshot = 20f;

    void WaitForScreenshot()
    {
        this.ParcelState = eParcelState.LoadingParcel;
        string fullScreenshotPath = GetCurrentScreenshotPath();
        // check if screenshot was already taken
        if (System.IO.File.Exists(fullScreenshotPath))
        {
            Debug.Log("Screenshot already exists for coordinate (" + currentX * N + "," + currentY * N + ")");
            GoToNextParcel();
            return;
        }

        // check if waited too long (timeout)
        float waitedTime = Time.time - waitStartTime;
        bool timeoutExpired = waitedTime > waitTimeout;

        bool allScenesLoaded = true;
        // check if all scenes are loaded
        ParcelScene[] scenes = FindObjectsOfType<ParcelScene>();
        foreach (ParcelScene scene in scenes)
        {
            bool importantScene = true;            
            // for (int ix = 0; ix < N && !importantScene; ix++) {
            //     for (int iy = 0; iy < N && !importantScene; iy++) {
            //         int wx = currentX*N - Mathf.FloorToInt((N-1)/2) + ix;
            //         int wy = currentY*N - Mathf.FloorToInt((N-1)/2) + iy;
            //         // Debug.Log("\tchecking if current scene is important: " + scene.gameObject.name + " vs (" + wx + ", " + wy + ")");
            //         if (scene.gameObject.name.Contains("(" + wx + ", " + wy + ")")) {
            //             importantScene = true;
            //         }
            //     }
            // }
            if (!importantScene)
            {
                // Debug.Log("Scene " + scene.gameObject.name + " is not important, skipping...");
                continue;
            }
            // Debug.Log("Scene " + scene.gameObject.name + " is important, checking if loaded...");

            if (!scene.gameObject.name.Contains("ready!"))
            {
                Debug.Log(scene.gameObject.name + " is not loaded, waiting...");
                allScenesLoaded = false;
            }
        }

        if (allScenesLoaded || timeoutExpired)
        {
            if (allScenesLoaded)
            {
                Debug.Log("all scenes are loaded, preparing to take screenshot at (" + currentX * N + "," + currentY * N + ")");
            }
            if (timeoutExpired && !allScenesLoaded)
            {
                Debug.Log("Timeout waiting for screenshot at coordinate (" + currentX * N + "," + currentY * N + ")");
            }
            //Invoke("CameraCapture", waitBeforeScreenshot);
            CameraCapture("");
        }
        else
        {
            Debug.Log("waiting for screenshot at (" + currentX * N + "," + currentY * N + ") for " + waitedTime + " seconds");
            Invoke("WaitForScreenshot", this.ScreenShotTimeOut);
        }
    }

    string CameraCapture(string path, int retryCount = 3)
    {
        this.ParcelState = eParcelState.TakingSnapshot;
        string outputFilePath = path;

        try
        {
            Debug.Log("rendering output to texture");
            if (this.Camera.targetTexture.width != this.RenderSize.x || this.Camera.targetTexture.height != this.RenderSize.y)
            {
                this.Camera.targetTexture = new RenderTexture(this.RenderSize.x, this.RenderSize.y, 32);
            }

            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = this.Camera.targetTexture;

            this.Camera.Render();

            Texture2D Image = new Texture2D(this.Camera.targetTexture.width, this.Camera.targetTexture.height);
            Image.ReadPixels(new Rect(0, 0, this.Camera.targetTexture.width, this.Camera.targetTexture.height), 0, 0);
            Image.Apply();
            RenderTexture.active = currentRT;

            var Bytes = Image.EncodeToPNG();

            Destroy(Image);

            //if no file name was specified, create a new one
            if (outputFilePath == "")
            {
                outputFilePath = GetCurrentScreenshotPath();
            }
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputFilePath));

            Debug.Log($"output file {outputFilePath+".png"}");

            File.WriteAllBytes(outputFilePath + ".png", Bytes);
        }
        catch (System.Exception ex)
        {
            Debug.Log($"Unable to output file {outputFilePath + ".png"}");
        }
        if (this.Started)
        {
            if (System.IO.File.Exists(outputFilePath + ".png"))
            {
                Invoke("GoToNextParcel", this.GotoNextParcelTimeOut);
            }
            else
            {
                //For some reason we couldn't save the file! try again
                if (retryCount > 0) {
                    retryCount--;
                    CameraCapture(outputFilePath, retryCount);
                }
                
            }
            
        }

        return outputFilePath;
    }


    public void TakeSnapshot()
    {
        WaitForScreenshot();
    }

    private string GetCurrentScreenshotPath()
    {
        string outputFilePath = System.IO.Path.Combine(RenderOutputPath, $"{this.currentX.ToString("0")},{this.currentY.ToString("0")}");

        if (!OverwriteFiles) //we dont want to overwrite files: we try to get the first available file name
        {
            int count = 0;
            while (System.IO.File.Exists(outputFilePath + ".png"))
            {
                count++;
                outputFilePath = System.IO.Path.Combine(RenderOutputPath, $"{this.currentX.ToString("0")},{this.currentY.ToString("0")}_{count}");
            }
        }
        return outputFilePath;
    }
}
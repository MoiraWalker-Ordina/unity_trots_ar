using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class GenericVideoPlayer : MonoBehaviour
{
    public List<VideoByEnum> VideoUrls = new List<VideoByEnum>();
    private EnumVideo CurrentVideo = EnumVideo.BOAT;

    [SerializeField]
    private VideoPlayer HoloLensVideo;
    [SerializeField]
    private GameObject HoloLensVideoObject;

    private void Start()
    {
        if (FindObjectsOfType<GenericVideoPlayer>().Count() > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(this);
        }
    }

    public void SetCurrentVideo(EnumVideo videoCode)
    {
        CurrentVideo = videoCode;
    }

    public VideoClip GetCurrentVideo()
    {
        return VideoUrls.First( v => v.VideoCode == CurrentVideo).Video;
    }

    public void StartVideo()
    {
        if (Application.platform == RuntimePlatform.WSAPlayerARM)
        {
            HoloLensVideoObject.SetActive(true);
            HoloLensVideo.clip = GetCurrentVideo();
            HoloLensVideo.Play();
        }
        else
        {
            SceneManager.LoadScene("VideoPlayer");
        }
    }
}

[Serializable]
public class VideoByEnum
{
    public EnumVideo VideoCode;
    public VideoClip Video;
}

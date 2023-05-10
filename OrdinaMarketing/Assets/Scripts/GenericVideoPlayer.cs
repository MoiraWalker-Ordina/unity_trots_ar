using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GenericVideoPlayer : MonoBehaviour
{
    public List<VideoUrl> VideoUrls = new List<VideoUrl>();
    private EnumVideo CurrentVideo = EnumVideo.BOAT;

    public void SetCurrentVideo(EnumVideo videoCode)
    {
        CurrentVideo = videoCode;
    }

    public void StartVideo()
    {
        if (Application.platform == RuntimePlatform.WSAPlayerARM)
        {

        }
        else
        {
            var url = VideoUrls.FirstOrDefault(v => v.VideoCode == CurrentVideo)?.Url ?? "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            Application.OpenURL(url);
        }
    }
}

[Serializable]
public class VideoUrl
{
    public EnumVideo VideoCode;
    public string Url;
}

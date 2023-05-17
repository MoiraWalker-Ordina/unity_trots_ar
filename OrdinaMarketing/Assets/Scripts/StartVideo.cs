using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class StartVideo : MonoBehaviour
{
    void Start()
    {
        var videoPlayer = GetComponent<VideoPlayer>();
        var videoManager = FindObjectOfType<GenericVideoPlayer>();
        if(videoManager != null && videoPlayer != null)
        {
            videoPlayer.clip = videoManager.GetCurrentVideo();
            videoPlayer.Play();
        }
    }

}

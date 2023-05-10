using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserDetector : MonoBehaviour
{
    [SerializeField]
    private EnumVideo VideoToPlay = EnumVideo.BOAT;
    [SerializeField]
    private GameObject VideoPlayButton;
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("enter trigger");
        var videoPlayer = FindObjectOfType<GenericVideoPlayer>();
        if(videoPlayer != null)
        {
            Debug.Log("found videoplayer");

            videoPlayer.SetCurrentVideo(VideoToPlay);
            VideoPlayButton.SetActive(true);

        }
        
    }

    private void OnTriggerExit(Collider other)
    {
        VideoPlayButton.SetActive(false);

    }
}

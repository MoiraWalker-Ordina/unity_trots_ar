using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class OnVideoEnd : MonoBehaviour
{
    [SerializeField]
    private VideoPlayer VideoPlayer;
    [SerializeField]
    private UnityEvent VideoEnded;

    private void Start()
    {
        VideoPlayer.loopPointReached += VideoEnd;
    }

    private void VideoEnd(VideoPlayer vp)
    {
        VideoEnded?.Invoke();
    }
}

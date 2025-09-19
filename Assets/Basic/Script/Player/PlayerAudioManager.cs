using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource _footstepsfx;
    [SerializeField]
    private AudioSource _landingsfx;
    [SerializeField]
    private AudioSource _punchsfx;
    [SerializeField]
    private AudioSource _glidesfx;

    private void PlayFootStepSfx()
    {
        _footstepsfx.volume = Random.Range(0.8f, 1f);
        _footstepsfx.pitch = Random.Range(0.8f, 1.5f);
        _footstepsfx.Play();
    }
    private void LandingSfx()
    {
        _landingsfx.Play();
    }
    private void PunchSfx()
    {
        _punchsfx.volume = Random.Range(0.8f, 1f);
        _punchsfx.pitch = Random.Range(0.8f, 1.5f);
        _punchsfx.Play();
    }
    public void PlayGlideSfx()
    {
        _glidesfx.Play();
    }
    public void StopGlideSfx()
    {
        _glidesfx.Stop();
    }
}

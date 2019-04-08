using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinCollision : MonoBehaviour
{
    // Start is called before the first frame update

    private Rigidbody pinRB;
    private AudioSource pinAudio;
    void Start()
    {
        pinRB = this.GetComponent<Rigidbody>();
        pinAudio = this.GetComponent<AudioSource>();
    }
    private void OnCollisionEnter(Collision col) {
        if (col.gameObject.name == "BowlingBall(Clone)") {
            pinAudio.Play();
            print("Play sound");
        }
    }
}

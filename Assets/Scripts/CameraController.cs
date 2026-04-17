// Unity's core features
using UnityEngine;

// This script makes the camera follow the player (ball) smoothly.
// We'll attach this script to the Main Camera.
public class CameraController : MonoBehaviour
{
    // A public reference to the player's GameObject.
    // We'll drag the Player (ball) into this slot in the Inspector.
    // This tells the camera WHO to follow.
    public GameObject player;

    // A private variable to store the distance between the camera
    // and the player at the START of the game.
    // "Vector3" stores three numbers: X, Y, and Z.
    // We calculate this once and then maintain this offset forever.
    private Vector3 offset;

    // Start() runs once when the game begins.
    void Start()
    {
        // Calculate the offset: Camera's position MINUS Player's position.
        //
        // Example:
        //   Camera is at (0, 10, -10)
        //   Player is at (0, 0.5, 0)
        //   Offset = (0-0, 10-0.5, -10-0) = (0, 9.5, -10)
        //
        // "transform.position" is THIS object's position (the camera).
        // "player.transform.position" is the player's position.
        offset = transform.position - player.transform.position;
    }

    // LateUpdate() is like Update(), but it runs AFTER all Update()
    // functions have finished.
    //
    // Why LateUpdate instead of Update?
    // Because the player's movement happens in FixedUpdate/Update.
    // If the camera moved at the same time, it might move BEFORE
    // the player does, causing jittery/shaky movement.
    // LateUpdate ensures the player moves first, THEN the camera
    // follows. This makes the camera movement silky smooth!
    void LateUpdate()
    {
        // Set the camera's position to the player's position PLUS the offset.
        //
        // This means wherever the player goes, the camera follows
        // at exactly the same distance.
        //
        // If the player is at (5, 0.5, 3) and offset is (0, 9.5, -10),
        // the camera will be at (5+0, 0.5+9.5, 3+(-10)) = (5, 10, -7).
        transform.position = player.transform.position + offset;
    }
}
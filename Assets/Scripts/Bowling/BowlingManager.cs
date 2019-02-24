using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.MagicLeap;

public class BowlingManager : MonoBehaviour {

	public enum holdState {
		none,
		single,
		tenPin,
		ball
	}
	public enum menuState {
		none,
		home,
		modifiers,
		changeBall
	}

	public static holdState holding = holdState.single;
	public static menuState currentMenuState;
	private MLInputController controller;
	public GameObject mainCam, orientationCube, control, tenPinOrientation, ballPrefab, menu, ballMenu, modifierMenu, tutorialMenu;
	public static GameObject menuControl;
	private GameObject bowlingBall;
	public Material transparent, activeMat;
	public Material[] ballMats;
	public Transform singlePrefab, tenPinPrefab, pinHolder, singleNoGravityPrefab, tenPinNoGravityPrefab;
	public LineRenderer laserLineRenderer;
	private Vector3 endPosition, forcePerSecond;
	List<Vector3> Deltas = new List<Vector3> ();
	private float timeHold = 3.0f, totalObjs = 0, objLimit = 50;
	private Controller checkController;
	public static float growSpeed;
	public static string ballColor;
	private bool setHand = false, placed = false, holdingBall = false, menuOpened = false, ballMenuOpened = false, holdingBallMenu = true, noGravity = false, tutorialActive = true, tutorialBumperPressed, tutorialHomePressed;
	private static bool menuClosed = false;
	// Use this for initialization
	void Start () {
		CheckNewUser();
		MLInput.Start ();

		growSpeed = 5f;

		controller = MLInput.GetController (MLInput.Hand.Left);
		MLInput.OnControllerButtonDown += OnButtonDown;

		// Initialize both line points at Vector3.Zero
		Vector3[] initLaserPositions = new Vector3[2] { Vector3.zero, Vector3.zero };
		laserLineRenderer.SetPositions (initLaserPositions);

		menuControl = GameObject.Find ("ObjectMenu");

		checkController = control.GetComponentInChildren<Controller> ();

		// TODO: Double check that this line is not needed as ball colors are called when balls are spawned
		// BowlingColorLoader.LoadBallColor (bowlingBall, ballMats);

	}
	private void OnDestroy () {
		MLInput.Stop ();
	}

	// Update is called once per frame
	void Update () {
		control.transform.position = controller.Position;
		control.transform.rotation = controller.Orientation;
		if (tutorialActive == false) {
			SetLine ();
			PlaceObject ();
		} else {
			if (controller.Touch1Active || controller.TriggerValue >= 0.2f || tutorialBumperPressed == true || tutorialHomePressed == true) {
				CheckNewUser();
			}
		}
		if (controller.Touch1Active) {
			if (menuClosed == false) {
				menuControl.SetActive (true);
			}
			if (setHand == false) {
				setHand = true;
				Vector3[] zero = new Vector3[2] { Vector3.zero, Vector3.zero };
				laserLineRenderer.SetPositions (zero);
				menuControl.transform.position = controller.Position;
				menuControl.transform.rotation = mainCam.transform.rotation;
			}
		}
		if (controller.Touch1Active == false) {
			menuClosed = false;
			setHand = false;
			menuControl.SetActive (false);
		}
		if (checkController.bumperTimer.getTime () >= timeHold) {
			ClearAllObjects ();
		}


	}
	private void SetLine () {
		RaycastHit rayHit;
		Vector3 heading = control.transform.forward;

		// Set the origin of the line to the controller's position.  Occurs every frame
		laserLineRenderer.SetPosition (0, controller.Position);

		if (Physics.Raycast (controller.Position, heading, out rayHit, 10.0f)) {
			// If the ray hits an object, set the line's end position to the distance between the controller and that point
			endPosition = controller.Position + (control.transform.forward * rayHit.distance);
			laserLineRenderer.SetPosition (1, endPosition);
			if (rayHit.transform.gameObject.name == "Home" && controller.TriggerValue >= 0.9f) {
				SceneManager.LoadScene ("Main", LoadSceneMode.Single);
			} else if (rayHit.transform.gameObject.name == "ChangeBall" && controller.TriggerValue >= 0.9f) {
				bowlingBall = Instantiate (ballPrefab, new Vector3 (100, 100, 100), tenPinOrientation.transform.rotation, pinHolder);
				ballMenu.transform.position = mainCam.transform.position + (mainCam.transform.forward * 1.5f);
				ballMenu.transform.rotation = mainCam.transform.rotation;
				ballMenu.SetActive (true);
				menuClosed = true;
				menu.SetActive (false);
				ballMenuOpened = true;
				holdingBallMenu = true;
			} else if (rayHit.transform.gameObject.name == "Modifiers" && controller.TriggerValue >= 0.9f) {
				modifierMenu.SetActive (true);
				menu.SetActive (false);
				menuClosed = true;
			} else if (rayHit.transform.gameObject.name == "NoGravity" && controller.TriggerValue >= 0.9f) {
				if (noGravity) {
					noGravity = false;
				}  else {
					noGravity = true;
				}
				modifierMenu.SetActive (false);
			}
			if (!holdingBallMenu) {
				BowlingColorLoader.GetBallColor (rayHit, controller, ballMenu, ballMenuOpened, holdingBallMenu, bowlingBall, ballMats);
			} else if (holdingBallMenu && controller.TriggerValue <= 0.2f) {
				holdingBallMenu = false;
			}

		} else {
			// If no object is hit, make the length of the line 3 meters out from the controller
			endPosition = controller.Position + (control.transform.forward * 3.0f);
			laserLineRenderer.SetPosition (1, endPosition);
		}
	}
	private void PlaceObject () {
		if (holding == holdState.ball) {
			laserLineRenderer.material = transparent;
			if (controller.TriggerValue >= 0.9f) {
				if (holdingBall ) {
					HoldingBall ();
				} else {
					holdingBall = true;
					BowlingColorLoader.LoadBallColor (bowlingBall, ballMats);
				}
			}
		} else if (holding == holdState.single) {
			laserLineRenderer.material = activeMat;
		} else if (holding == holdState.tenPin) { 
			laserLineRenderer.material = activeMat;
		} else if (holding == holdState.none) {
			laserLineRenderer.material = transparent;
		}
		if (controller.TriggerValue >= 0.9f) {
			if (placed == false) {
				SpawnObject ();
				placed = true;	
			}
		} else if (controller.TriggerValue <= 0.2f) {
			if (holdingBall == true) {
				holdingBall = false;
				var rigidbody = bowlingBall.GetComponent<Rigidbody> ();
				// Enable the rigidbody on the ball, then apply current forces to the ball
				rigidbody.useGravity = true;
				rigidbody.velocity = Vector3.zero;
				rigidbody.velocity = forcePerSecond;
			} else {
				placed = false;
			}
		}
	}
	private void HoldingBall () {
		var oldPosition = bowlingBall.transform.position;
		var newPosition = controller.Position;

		var delta = newPosition - oldPosition;
		if (Deltas.Count == 15) {
			Deltas.RemoveAt (0);
		}
		Deltas.Add (delta);
		Vector3 toAverage = Vector3.zero;
		foreach (var toAdd in Deltas) {
			toAverage += toAdd;
		}
		toAverage /= Deltas.Count;
		var forcePerSecondAvg = toAverage * 120;
		forcePerSecond = forcePerSecondAvg;
		bowlingBall.transform.position = controller.Position;
	}

	public static void CloseMenu () {
		menuClosed = true;
		menuControl.SetActive (false);
	}

	private void ClearAllObjects () {
		foreach (Transform child in pinHolder.transform) {
			GameObject.Destroy (child.gameObject);
			totalObjs = 0;
		}
	}

	void OnButtonDown (byte controller_id, MLInputControllerButton button) {
		holding = holdState.none;
		if (button == MLInputControllerButton.HomeTap && tutorialActive == true) {
			tutorialHomePressed = true;
		} else if (button == MLInputControllerButton.Bumper && tutorialActive == true) {
			tutorialBumperPressed = true;
		} else if (button == MLInputControllerButton.HomeTap && menuOpened == false) {
			// When the user presses the Home button and the menu is not opened, then open the menu
			laserLineRenderer.material = activeMat;
			menu.SetActive (true);
			modifierMenu.SetActive (false);
			menuOpened = true;
		} else if (button == MLInputControllerButton.HomeTap && menuOpened == true) {
			// If the user presses the Home button and the menu is opened, then close the menu
			menu.SetActive (false);
			menuOpened = false;
		} 
	}
	private void SpawnObject () {
		// Set a limit as to how many objects can be spawned so framerate will not suffer
		if (totalObjs < objLimit) {
			// Check to see if the user has enabled the noGravity modifier
			if (!noGravity) {
				if (holding == holdState.single) {
					Instantiate (singlePrefab, endPosition, orientationCube.transform.rotation, pinHolder);
				} else if (holding == holdState.tenPin) {
					Instantiate (tenPinPrefab, endPosition, tenPinOrientation.transform.rotation, pinHolder);
				} else if (holding == holdState.ball) {
					//Spawn ball, then set color based on PlayerPrefs value
					holdingBall = true;
					bowlingBall = Instantiate (ballPrefab, controller.Position, tenPinOrientation.transform.rotation, pinHolder);
					Transform ballObject = bowlingBall.transform.GetChild (0);
					Renderer ballMat = ballObject.GetComponent<Renderer> ();
					ballMat.material = ballMats[PlayerPrefs.GetInt ("ballColorInt", 0)];
				}
			} else if (noGravity) {
				if (holding == holdState.single) {
					Instantiate (singleNoGravityPrefab, endPosition, orientationCube.transform.rotation, pinHolder);
				} else if (holding == holdState.tenPin) {
					Instantiate (tenPinNoGravityPrefab, endPosition, tenPinOrientation.transform.rotation, pinHolder);
				} else if (holding == holdState.ball) {
					//Spawn ball, disable gravity for noGravity state, then set color based on PlayerPrefs value
					holdingBall = true;
					bowlingBall = Instantiate (ballPrefab, controller.Position, tenPinOrientation.transform.rotation, pinHolder);
					Rigidbody ballRB = bowlingBall.GetComponent<Rigidbody> ();
					ballRB.useGravity = false;
					Transform ballObject = bowlingBall.transform.GetChild (0);
					Renderer ballMat = ballObject.GetComponent<Renderer> ();
					ballMat.material = ballMats[PlayerPrefs.GetInt ("ballColorInt", 0)];
				}
			}
		} else {
			print("Too many objects");
		}
		// Get a count of how many objects there are to ensure that there are not too many objects at once
		GetCount();
	}
	private void GetCount() {
		totalObjs = 0;
		foreach (Transform bowlObj in pinHolder) {
			Transform objectstotal = bowlObj.GetComponentInChildren<Transform>();
			totalObjs += objectstotal.childCount;
		}
	}
	private void CheckNewUser() {
		if (PlayerPrefs.GetInt("hasPlayedBowling") == 1) {
			print("Played");
			tutorialActive = false;
			tutorialMenu.SetActive(false);
		} else {
			print("Not Played");
			tutorialMenu.SetActive(true);
			PlayerPrefs.SetInt("hasPlayedBowling", 1);
		}
	}
}
using UnityEngine;
using System.Collections;

public class ToggleDoorRotateBehaviour : MonoBehaviour
{
    public GameObject door;
    public OculusSampleFramework.DistanceGrabbable grabbableWand;
    public GameObject orcSpider;
    public GameObject orcPig;

    public AudioClip introductionSound; // The introduction sound for guiding how to play the game
    private AudioSource introductionAudioSource; // Separate AudioSource for intrroduction
    public AudioClip backgroundSound; // The sound to play in the background when gripping the wand
    private AudioSource backgroundAudioSource; // Separate AudioSource for background sound to allow looping
    public AudioClip transformationBackSound; // The sound to play when transforming back to the pig
    private AudioSource audioSource; // The AudioSource component
    private bool canReset = true; // A flag to control when the ResetTransformation can be called

    public float rotationSpeed = 1.0f;
    private bool isWandGrabbed = false;
    private float targetYRotation;
    private float initialYRotation;

    public float moveDistance = 5.0f;
    public float spiderMoveSpeed = 1.0f;
    private Vector3 orcSpiderOriginalPosition;
    private Vector3 neworcSpiderPosition;

    public float jumpHeight = 0.5f; // The height of the Orc-Spider's jumps
    public float jumpSpeed = 1.0f; // The speed of the Orc-Spider's jumps

    void Start()
    {
        if (door != null)
        {
            initialYRotation = door.transform.eulerAngles.y;
            targetYRotation = initialYRotation;
        }

        if (grabbableWand != null)
        {
            grabbableWand.onGrabBegin.AddListener(OnWandGrabbed);
            grabbableWand.onGrabEnd.AddListener(OnWandReleased);
        }

        if (orcSpider != null)
        {
            orcSpiderOriginalPosition = orcSpider.transform.position;
        }


        // Initialize the AudioSource component
        audioSource = gameObject.AddComponent<AudioSource>();

        // Initialize a separate AudioSource for the background sound
        backgroundAudioSource = gameObject.AddComponent<AudioSource>();
        backgroundAudioSource.loop = true; // Set to loop the background sound

        // Initialize a separate AudioSource for the introduction sound
        introductionAudioSource = gameObject.AddComponent<AudioSource>();
        // Play the voice sound at the beginning of the game
        if (introductionSound != null && introductionAudioSource != null)
        {
            introductionAudioSource.clip = introductionSound;
            introductionAudioSource.Play();
        }
    }

    void OnWandGrabbed()
    {
        isWandGrabbed = true;
        targetYRotation = initialYRotation + 180.0f;

        // Play the background sound
        if (backgroundSound != null && backgroundAudioSource != null)
        {
            backgroundAudioSource.clip = backgroundSound;
            backgroundAudioSource.Play();
        }

        if (orcSpider != null)
        {
            orcSpider.SetActive(true); // Reactivate the orc spider
            neworcSpiderPosition = orcSpider.transform.position + Vector3.forward * moveDistance; // Use global forward direction
            StopCoroutine("MoveSpider");
            StartCoroutine(MoveSpider(neworcSpiderPosition, true)); // Start the move coroutine
        }
    }

    void OnWandReleased()
    {
        isWandGrabbed = false;
        targetYRotation = initialYRotation;

        if (orcSpider != null)
        {
            StopCoroutine("MoveSpider");
            StopCoroutine("TransformationSequence"); // Stop the transformation sequence if it's running
            neworcSpiderPosition = orcSpiderOriginalPosition; // Reset the target position to the original position
            StartCoroutine(MoveSpider(neworcSpiderPosition, false)); // Start the move coroutine to move back to the original position
        }

        if (orcPig != null)
        {
            orcPig.SetActive(false);
        }

        // Stop the background sound
        if (backgroundAudioSource != null)
        {
            backgroundAudioSource.Stop();
        }
    }


    void Update()
    {
        if (door != null)
        {
            float yRotation = Mathf.Lerp(door.transform.eulerAngles.y, targetYRotation, Time.deltaTime * rotationSpeed);
            door.transform.eulerAngles = new Vector3(door.transform.eulerAngles.x, yRotation, door.transform.eulerAngles.z);
        }

        if (isWandGrabbed && OVRInput.GetDown(OVRInput.Button.Three) && canReset)
        {
            StartCoroutine(ResetTransformation());
        }
    }

    private void OnDestroy()
    {
        if (grabbableWand != null)
        {
            grabbableWand.onGrabBegin.RemoveListener(OnWandGrabbed);
            grabbableWand.onGrabEnd.RemoveListener(OnWandReleased);
        }
    }

    private IEnumerator MoveSpider(Vector3 targetPosition, bool transformSequence)
    {
        while (Vector3.Distance(orcSpider.transform.position, targetPosition) > 0.01f)
        {
            orcSpider.transform.position = Vector3.MoveTowards(orcSpider.transform.position, targetPosition, spiderMoveSpeed * Time.deltaTime);
            yield return null;
        }
        orcSpider.transform.position = targetPosition;

        if (transformSequence)
        {
            // Start the jump sequence after reaching the target position
            StartCoroutine(TransformationSequence());
        }
    }

    private IEnumerator TransformationSequence()
    {
        // Make the Orc-Spider jump up and down twice
        for (int i = 0; i < 2; i++)
        {
            yield return Jump(jumpHeight, jumpSpeed);
            yield return Jump(0, jumpSpeed);
        }
    }

    private IEnumerator Jump(float targetHeight, float speed)
    {
        float startY = orcSpider.transform.position.y;
        float targetY = startY + targetHeight;

        while (Mathf.Abs(orcSpider.transform.position.y - targetY) > 0.01f)
        {
            float newY = Mathf.MoveTowards(orcSpider.transform.position.y, targetY, speed * Time.deltaTime);
            orcSpider.transform.position = new Vector3(orcSpider.transform.position.x, newY, orcSpider.transform.position.z);
            yield return null;
        }
    }

    private IEnumerator ResetTransformation()
    {
        canReset = false; // Block further reset until this one is complete

        // Play the sound effect
        if (transformationBackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(transformationBackSound);
        }

        // Wait for 3 seconds
        yield return new WaitForSeconds(3f);

        // Reset the transformation and change the game object back to the pig
        if (orcPig != null)
        {
            orcPig.transform.position = orcSpider.transform.position; // Set the pig's position to the spider's current position
            orcPig.SetActive(true); // Activate the pig
        }
        else
        {
            Debug.LogError("orcPig reference is not set in the inspector");
        }

        if (orcSpider != null)
        {
            orcSpider.SetActive(false); // Deactivate the spider
        }

        canReset = true; // Allow reset to be called again
    }
}

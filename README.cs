using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DemoSpriteSequenceAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 24f;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool loop = true;

    private Image targetImage;
    private float timer;
    private int currentFrame;
    private bool isPlaying;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            PlayFromStart();
        }
    }

    private void OnDisable()
    {
        isPlaying = false;
    }

    private void Update()
    {
        if (!isPlaying || frames == null || frames.Length == 0)
        {
            return;
        }

        timer += Time.deltaTime;

        float frameDuration = 1f / framesPerSecond;

        while (timer >= frameDuration)
        {
            timer -= frameDuration;
            GoToNextFrame();
        }
    }

    public void PlayFromStart()
    {
        currentFrame = 0;
        timer = 0f;
        isPlaying = true;
        ApplyFrame();
    }

    public void Stop()
    {
        isPlaying = false;
    }

    private void GoToNextFrame()
    {
        currentFrame++;

        if (currentFrame >= frames.Length)
        {
            if (loop)
            {
                currentFrame = 0;
            }
            else
            {
                currentFrame = frames.Length - 1;
                isPlaying = false;
            }
        }

        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (targetImage == null || frames == null || frames.Length == 0)
        {
            return;
        }

        targetImage.sprite = frames[currentFrame];
        targetImage.preserveAspect = true;
    }
}

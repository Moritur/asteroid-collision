using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager singleton = null;
#pragma warning disable CS0649
    [SerializeField][Tooltip("UI displayed when player loses")]
    GameObject GameOverUI;
    [SerializeField][Tooltip("Text component used to display score")]
    Text scoreTextField;
    [SerializeField][Tooltip("Text preceding score (e.g. \"Score:\")")]
    string scoreText;
#pragma warning restore CS0649

    int score;
    public int Score
    {
        get { return score; }
        private set
        {
            score = value;
            scoreTextField.text = scoreText + score;
        }
    }

    //used to reset positions of all objects without reloading scene
    public event System.Action RestartGame;

    void Awake()
    {
        if(singleton == null)
        {
            singleton = this;
        }else if(singleton != this)
        {
            Destroy(gameObject);
        }
        GameOverUI.SetActive(false);
        scoreTextField.text = scoreText + 0;
    }

    //called by ShipControl after collision with asteroid
    public void GameOver()
    {
        CirclePhysicsManager.singleton.simulate = false;    //stop collision detection
        Time.timeScale = 0f;                                //stop asteroid movement
        GameOverUI.SetActive(true);
    }

    //called when player clicks "RESTART" button
    public void OnRestartGame()
    {
        RestartGame();
        GameOverUI.SetActive(false);
        Score = 0;
        Time.timeScale = 1f;
    }

    //called by Bullet when it destroys asteroid
    public void AddScore()
    {
        Score++;
    }
}

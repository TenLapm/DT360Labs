using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoccerEnvController : MonoBehaviour
{
    [System.Serializable]
    public class PlayerInfo
    {
        public AgentSoccer Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
    }


    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;

    /// <summary>
    /// The area bounds.
    /// </summary>

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>

    public GameObject ball;
    [HideInInspector]
    public Rigidbody ballRb;
    Vector3 m_BallStartingPos;

    //List of Agents On Platform
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();

    private SoccerSettings m_SoccerSettings;


    private SimpleMultiAgentGroup m_BlueAgentGroup;
    private SimpleMultiAgentGroup m_PurpleAgentGroup;

    public int match_time;
    private int m_ResetTimer;
    private int m_BlueScore;
    private int m_PurpleScore;
    private float m_Clock;
    private int scored;
    private bool spacebarResume;
    private bool blinkingText;
    public TMP_Text clockText; 
    public TMP_Text scoreBlueText; 
    public TMP_Text scorePurpleText; 

    void Start()
    {
        m_Clock = (float)(match_time + 1);
        m_BlueScore = 0;
        m_PurpleScore = 0;
        scored = 0;
        spacebarResume = true;
        blinkingText = false;

        m_SoccerSettings = FindFirstObjectByType<SoccerSettings>();
        // Initialize TeamManager
        m_BlueAgentGroup = new SimpleMultiAgentGroup();
        m_PurpleAgentGroup = new SimpleMultiAgentGroup();
        ballRb = ball.GetComponent<Rigidbody>();
        m_BallStartingPos = new Vector3(ball.transform.position.x, ball.transform.position.y, ball.transform.position.z);
        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            if (item.Agent.team == Team.Blue)
            {
                m_BlueAgentGroup.RegisterAgent(item.Agent);
            }
            else
            {
                m_PurpleAgentGroup.RegisterAgent(item.Agent);
            }
        }
        if (!Academy.Instance.IsCommunicatorOn)
        {
            SpacebarToContinue();
        }
        ResetScene();
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1;

        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_BlueAgentGroup.GroupEpisodeInterrupted();
            m_PurpleAgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    
        m_Clock -= Time.deltaTime;

        // game over
        if (m_Clock <= 0)
        {
            EndMatch();
            return;
        }

        // Scoreboard update
        if (blinkingText) return; // skip update if text is blinking
        if (scored != 0)
        {
            switch(scored)
            {
                case 1:
                    clockText.text = "GOAL! Blue Scores!";
                    break;
                case 2:
                    clockText.text = "GOAL! Purple Scores!";
                    break;
            }
            StartCoroutine(BlinkTextRoutine(clockText));
            blinkingText = true;
        }
        else if (spacebarResume)
        {
            clockText.text = "Press Spacebar to Resume!";
        }
        else {
            int minutes = Mathf.FloorToInt(m_Clock / 60);
            int seconds = Mathf.FloorToInt(m_Clock % 60);
            clockText.text = string.Format("{0:00}:{1:00}", minutes, seconds);   // Result: "02:05"   
        }
    }


    public void ResetBall()
    {
        ball.transform.position = m_BallStartingPos;
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
    }

    public void GoalTouched(Team scoredTeam)
    {
        // avoid multiple touches on the single goal due to ball delay
        if (scored != 0) return;

        // first touch = goal.
        if (scoredTeam == Team.Blue)
        {
            m_BlueAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            m_PurpleAgentGroup.AddGroupReward(-1);

            m_BlueScore++;
            scoreBlueText.text = m_BlueScore.ToString();

            scored = 1;
        }
        else
        {
            m_PurpleAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            m_BlueAgentGroup.AddGroupReward(-1);

            m_PurpleScore++;
            scorePurpleText.text = m_PurpleScore.ToString();

            scored = 2;
        }

        if (scored != 0)
        {
            Time.timeScale = 0.35f;
            StartCoroutine(DelayedReset(1.0f));
        }
    }

    IEnumerator DelayedReset(float delay)
    {
        yield return new WaitForSeconds(delay);

        Time.timeScale = 1.0f;

        m_PurpleAgentGroup.EndGroupEpisode();
        m_BlueAgentGroup.EndGroupEpisode();
        ResetScene();
        scored = 0;
    }

    public void ResetScene()
    {
        m_ResetTimer = 0;

        //Reset Agents
        foreach (var item in AgentsList)
        {
            var randomPosX = Random.Range(-5f, 5f);
            var newStartPos = item.Agent.initialPos + new Vector3(randomPosX, 0f, 0f);
            var rot = item.Agent.rotSign * Random.Range(80.0f, 100.0f);
            var newRot = Quaternion.Euler(0, rot, 0);
            item.Agent.transform.SetPositionAndRotation(newStartPos, newRot);

            item.Rb.linearVelocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
        }

        //Reset Ball
        ResetBall();
    }

    IEnumerator BlinkTextRoutine(TMP_Text myText)
    {
        float duration = 1.0f; // Total time
        float endTime = Time.time + duration;

        // Loop until 1 second has passed
        while (Time.time < endTime)
        {
            myText.enabled = !myText.enabled; // Toggle visibility
            yield return new WaitForSeconds(0.25f); // Speed of blink
        }

        myText.enabled = true; // Ensure it appears at the end
        blinkingText = false;
    }

    void EndMatch()
    {
        // isGameActive = false;
        if (Academy.Instance.IsCommunicatorOn)
        {
            m_BlueAgentGroup.GroupEpisodeInterrupted();
            m_PurpleAgentGroup.GroupEpisodeInterrupted();
            ResetScene();
            m_Clock = (float)(match_time + 1); // Reset the match timer
            return;
        }
        // Determine winner (replace with your actual score variables)
        string winner = m_PurpleScore > m_BlueScore ? "PURPLE WINS!" : "BLUE WINS!";
        if (m_PurpleScore ==m_BlueScore) winner = "IT'S A DRAW!";

        clockText.text = winner;
        clockText.enabled = true;

        // Freeze physics so players stop moving
        foreach (var item in AgentsList)
        {
            item.Rb.linearVelocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
        }
        Time.timeScale = 0.0f;
    }

    void SpacebarToContinue()
    {
        spacebarResume = true;
        Time.timeScale = 0.0f;
        StartCoroutine(WaitForSpacebar());
    }

    IEnumerator WaitForSpacebar()
    {
        // while (Keyboard.current.spaceKey.wasPressedThisFrame)

        while (!Input.GetKeyDown(KeyCode.Space))
        {
            yield return null; // Wait until the next frame
        }
        Time.timeScale = 1.0f; // Resume the game
        spacebarResume = false;
    }
}

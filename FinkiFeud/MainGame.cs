﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FinkiFeud
{
    public partial class MainGame : Form
    {

        public static int questionCounter = 1;
        public static List<int> questionIndex = new List<int>();

        private int counter = 0;
        private bool flag = false;
        private static int Time = 60;

        public static bool gameFinished = false;
        public static List<Player> players = new List<Player>();
        public static MazePlayer mazePlayer;
        public static int finalScore;

        private PlayerDoc mainGame;
        //How much points will the answer give the player
        private int answerPoints;
        
        private int flagPoints = 0;
        private Player currentPlayer;
        //String FileName;

        //pause/continue button 
        private Boolean pauseFlag = false;


        //This list will contain the answers of the questions
        private List<String> answers = new List<String>();

        //SoundPlayers
        private SoundPlayer RevealSoundPlayer = new SoundPlayer();
        private SoundPlayer TryAgainSoundPlayer = new SoundPlayer();
        private SoundPlayer WrongSoundPlayer = new SoundPlayer();

        //List for saving all the answers the user has already tried that were correct
        private List<String> alreadyAnswered = new List<String>();

        public MainGame()
        {
            //Show a loading Screen before starting the game
            SplashScreen splash = new SplashScreen();
            splash.ShowDialog();

            //Initialize the Component
            InitializeComponent();
            currentPlayer = new Player(ChooseGame.player.Points, ChooseGame.player.Name, ChooseGame.player.Difficulty, ChooseGame.player.PlayerIcon);
            mainGame = new PlayerDoc(currentPlayer);
            mainGame.getTimeFromDifficulty();
            Time = mainGame.Time;
            mainGame.getScoreMultiplier();

            //Resolve Flickering
            this.DoubleBuffered = true;

            //Load the first question
            nextQuestion();

            //The timer initialization
            timer1.Start();

            //Full Screen mode
            if (ChooseGame.windowedMode == false)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }

            //Music and Sound
            if (ChooseGame.backgroundMusic == true)
            {
                Stream Theme = Properties.Resources.TV_Theme_Song;
                SoundPlayer ThemeSoundPlayer = new SoundPlayer(Theme);
                ThemeSoundPlayer.LoadAsync();
                ThemeSoundPlayer.Play();
            }
            if (ChooseGame.backgroundSound==true)
            {
                Stream Reveal = Properties.Resources.CLANG__reveal_answer_on_board;
                Stream TryAgain = Properties.Resources.Try_Again1;
                Stream Wrong = Properties.Resources.STRIKE__wrong_answer;
                RevealSoundPlayer = new SoundPlayer(Reveal);
                TryAgainSoundPlayer = new SoundPlayer(TryAgain);
                WrongSoundPlayer = new SoundPlayer(Wrong);
                RevealSoundPlayer.LoadAsync();
                TryAgainSoundPlayer.LoadAsync();
                WrongSoundPlayer.LoadAsync();
            }

            //current player
         
            tbPlayerName.Text = currentPlayer.Name;
            pbPlayerIcon.Image = currentPlayer.PlayerIcon;
            
        }

        public void addPlayerInText()
        {
            //get new info
            String name = mainGame.MainPlayer.Name;
            String points = mainGame.Score.ToString();
            String difficulty = mainGame.MainPlayer.Difficulty;
            String playersInfo = File.ReadAllText("Players.txt");
            String playerInfo = name + "-" + points + "-" + difficulty;
            String final = playersInfo + "~" + playerInfo;

            //write
            using (StreamWriter sw = File.CreateText("Players.txt"))
            {
                sw.Write(final);
            }
        }
        public void gameDone()
        {
            //close game
            timer1.Stop();

            mazePlayer = new MazePlayer(mainGame.Score, mainGame.MainPlayer.Name, mainGame.MainPlayer.Difficulty, mainGame.MainPlayer.PlayerIcon, new Point(0, 0));

            MazeFinale mazeGame = new MazeFinale();
            this.Hide();
            if (mazeGame.ShowDialog() == DialogResult.Cancel)
            {
                if (MazeFinale.atEnd == true)
                {
                    mainGame.passedMaze = true;
                    mainGame.updateScore(0);
                    finalScore = mainGame.Score;
                    mazeGame.Close();
                }
            }
            if (MazeFinale.atEnd==true)
            {
                Win gameWon = new Win();
                this.Hide();
                if (gameWon.ShowDialog() == DialogResult.Cancel)
                {
                    addPlayerInText();
                    gameFinished = true;
                    this.Close();
                }
            }
            else
            {
                this.Hide();
                Lose gameLost = new Lose();
                if (gameLost.ShowDialog() == DialogResult.Cancel)
                {
                    mainGame.Score = 0;
                    gameFinished = true;
                    this.Close();
                }
            }
        }
        public void nextQuestion()
        {
            resetImages();
            //read questions txt
            String questionsFile = File.ReadAllText("Questions.txt");

            //split questions
            String[] questionsDivided = questionsFile.Split(new string[] { "~" }, StringSplitOptions.None);
            questionCounter = questionsDivided.Length - 1;

            //randomize questions
            Random random = new Random();
            int number;
            if (flag == false)
            {
                for (int i = 0; i < questionCounter; i++)
                {
                    do
                    {
                        number = random.Next(0, questionCounter);
                    } while (questionIndex.Contains(number));
                    questionIndex.Add(number);
                }
                flag = true;
            }

            //split question from answers
            String[] divided = questionsDivided[questionIndex[counter]].Split(new string[] { "\n" }, StringSplitOptions.None);

            //display questions and answers in appropriate places
            if (questionIndex[counter] == 0)
            {
                tbQuestion.Text = divided[0];
                //Add all the answers into the list
                for (int i = 1; i < divided.Length; i++)
                {
                    answers.Add(divided[i]);
                }
            }
            else
            {
                tbQuestion.Text = divided[1];
                //Add all the answers into the list
                for (int i = 2; i < divided.Length; i++)
                {
                    answers.Add(divided[i]);
                }
            }

            //counter and reset questions order
            counter++;
            if (counter == questionCounter)
            {
                flag = false;
                gameDone();
            }
        }
        
        private void btnSubmit_Click(object sender, EventArgs e)
        {
            bool noAnswerFound = true;
            String triedAnswer = tbAnswer.Text.Trim();//Contains the answer entered by the user once the button is clicked

            if (!(tbAnswer.Text == "/"))
            {
                //Check if the answer was already entered before, so the player doesn't get double the points for writing the same answer
                    if (alreadyAnswered.Contains(triedAnswer.ToLower()))
                    {
                        answerPoints = 0;
                        tbAnswer.Text = "";
                        if (TryAgainSoundPlayer.IsLoadCompleted == true)
                        {
                            TryAgainSoundPlayer.Play();
                        }
                    }
                    else
                    {
                        //Check if the answer the user has entered is anywhere in the list answers
                        foreach (String answer in answers)
                        {

                            //If the answer is anywhere in the list find the index of where the answer is in the list and place it in the appropriate field in the form
                            if (triedAnswer.ToLower() == answer.Trim().ToLower()) //changed from Contains to equals because it accepted uncompleted answers
                            {
                                if(RevealSoundPlayer.IsLoadCompleted == true)
                                {
                                    RevealSoundPlayer.Play();
                                }

                                //Add the correct triedAnswer into the list for checking if it was added before as well
                                alreadyAnswered.Add(triedAnswer.ToLower());
                                noAnswerFound = false;
                                int index = answers.IndexOf(answer);

                                //Place it into the appropriate field on the form and add in the points for the player.
                                if (index == 0)
                                {
                                    answer1.Text = answer;
                                    answer1.Image = null;
                                    answerPoints = 12;

                                }
                                if (index == 1)
                                {
                                    answer2.Text = answer;
                                    answer2.Image = null;
                                    answerPoints = 10;

                                }
                                if (index == 2)
                                {
                                    answer3.Text = answer;
                                    answer3.Image = null;
                                    answerPoints = 8;
                                }
                                if (index == 3)
                                {
                                    answer4.Text = answer;
                                    answer4.Image = null;
                                    answerPoints = 6;
                                }
                                if (index == 4)
                                {
                                    answer5.Text = answer;
                                    answer5.Image = null;
                                    answerPoints = 4;
                                }
                                if (index == 5)
                                {
                                    answer6.Text = answer;
                                    answer6.Image = null;
                                    answerPoints = 3;
                                }
                                if (index == 6)
                                {
                                    answer7.Text = answer;
                                    answer7.Image = null;
                                    answerPoints = 2;
                                }
                                if (index == 7)
                                {
                                    answer8.Text = answer;
                                    answer8.Image = null;
                                    answerPoints = 1;
                                }

                               
                                tbAnswer.Text = "";
                            }


                        }
                        if (noAnswerFound)
                        {
                            if (WrongSoundPlayer.IsLoadCompleted == true)
                            {
                                WrongSoundPlayer.Play();
                            }
                            tbAnswer.Text = "";

                            //If the answer was wrong then decrease the player's points by 1
                            if (flagPoints == 0)
                            {
                                flagPoints = 1;
                                if (mainGame.Score > 0)
                                    answerPoints=-1;
                            }
                        }
                    }
                    mainGame.updateScore(answerPoints);
                    lbPoints.Text = mainGame.Score.ToString();
                    flagPoints = 0;
                    answerPoints = 0;
                }
            
        }
        
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Time == 0)  //limit your timer according to the ticks the timer has done 300 ~= 35 seconds
            {
                //reset the timer, load a new question, clear the list with the tried answers
                timer1.Stop();
                answers.Clear();
                nextQuestion();
                timer1.Start();
                alreadyAnswered.Clear();

                //Set the time back
                Time = mainGame.Time;
                tbAnswer.Text = "";
            }
            Time--;
            questionTime.Text = Time.ToString();//Show the time left in the time label

            //Change the color to red if the time is almost up
            if (Time < 10)
            {
                questionTime.ForeColor = Color.Red;
            }
            else
            {
                questionTime.ForeColor = Color.White;
            }
        }

        //Resets all the answers so they are not visible to the player, only used when the nextQuestion function is triggered
        private void resetImages()
        {
            answer1.Text = "";
            answer1.Image = Properties.Resources.QuestionMark;
            answer2.Text = "";
            answer2.Image = Properties.Resources.QuestionMark;
            answer3.Text = "";
            answer3.Image = Properties.Resources.QuestionMark;
            answer4.Text = "";
            answer4.Image = Properties.Resources.QuestionMark;
            answer5.Text = "";
            answer5.Image = Properties.Resources.QuestionMark;
            answer6.Text = "";
            answer6.Image = Properties.Resources.QuestionMark;
            answer7.Text = "";
            answer7.Image = Properties.Resources.QuestionMark;
            answer8.Text = "";
            answer8.Image = Properties.Resources.QuestionMark;
        }

        //When the question is skipped reverse the time to 0
        private void btnSkip_Click(object sender, EventArgs e)
        {
            Time = 0;
        }

        private void btnFinish_Click(object sender, EventArgs e)
        {
            gameDone();
            

        }


        //Save the question before pausing 
        String saveQuestion = null;

        /*When the button is paused don't allow the user to see the question, 
        skip the question or submit and write an answer as it would be cheating 
        and stop the time so he can continue back to where he was before.*/
        public void Pause()
        {
                timer1.Stop();
                tbAnswer.ReadOnly = true;
                btnSubmit.Enabled = false;
                btnSkip.Enabled = false;
                saveQuestion = tbQuestion.Text;
                tbQuestion.Text = "";
                btnPause.Text = "CONTINUE";
                pauseFlag = true;            
        }

        /*When the game needs to continue allow the user to see the question, 
        skip the question or submit and write an answer 
        as he has decided to continue playing and resume the running time.*/
        public void Unpause()
        {
            timer1.Start();
            tbAnswer.ReadOnly = false;
            btnSubmit.Enabled = true;
            btnSkip.Enabled = true;
            tbQuestion.Text = saveQuestion;
            btnPause.Text = "PAUSE";
            pauseFlag = false;
        }

        //When clicking on the button to pause do the pause and unpause functions
        private void btnPause_Click(object sender, EventArgs e)
        {
            if (!pauseFlag)
            {
                Pause();
            }
            else
            {
                Unpause();
            }
            
        }

        
        private void MainGame_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ChooseGame.goBack == true) {
                e.Cancel = false;
            }
            //If the game is finished no need for a confimation as the game has alreaady ended for the player
            if(!gameFinished)
            {
                //Confirm on closing the game
                timer1.Stop();
                if (MessageBox.Show("If you close the game your progress will be lost. Are you sure you want to exit the game?", "Exiting program...",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.No)
                {
                    e.Cancel = true;
                    timer1.Start();
                }
            }
            else
            {
                
                e.Cancel = false;
            }
        }

        //Saving the game
        /*private void saveFile()
        {
            if (FileName == "Untitled")
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Circles doc file (*.crl)|*.crl";
                saveFileDialog.Title = "Save circles doc";
                saveFileDialog.FileName = FileName;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    FileName = saveFileDialog.FileName;
                }
            }
            if (FileName != null)
            {
                using (FileStream fileStream = new FileStream(FileName, FileMode.Create))
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(fileStream, currentPlayer);
                }
            }
        }*/
    }
}

﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Media3D;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TheAnnotator9000
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class MainPage : Page
    {
        private ESDRSGenerator g_ESDRSGenerator;
        private GestureAnnotation g_CurrentGesture;
        private GestureShape g_CurrentShape;
        private GestureMovement g_CurrentMovement;

        private Type g_VariableType;
        private Type g_PredicateType;
        private Type g_MappingType;
        private Type g_SpatiotemporalType;

        private JArray g_GestureDatabase;
        private JArray g_AnnotatedGesturesID;
        private StorageFolder g_RootFolder;
        private MediaPlayer g_MediaPlayer;
        private string g_SelectedGestureID;
        
        private string g_CurrentArm;
        private bool g_RightExists;
        private bool g_LeftExists;
        private int g_NumPlaceholders;
        private int g_CurrentPlaceholder;

        private bool g_FirstTimeIndVar;

        private int g_NodeID;
        private JArray g_Nodes;
        private JArray g_Edges;

        public MainPage()
        {
            this.InitializeComponent();

            g_ESDRSGenerator = new ESDRSGenerator();

            g_VariableType = typeof(Variable);
            g_PredicateType = typeof(Predicate);
            g_MappingType = typeof(Mapping);
            g_SpatiotemporalType = typeof(Spatiotemporal);

            GreetingMessageText.Visibility = Visibility.Visible;
            UsernameTextbox.Visibility = Visibility.Visible;
            PasswordTextbox.Visibility = Visibility.Visible;
            ButtonLogin.Visibility = Visibility.Visible;

            GestureIDTextbox.Visibility = Visibility.Collapsed;
            GestureIDButton.Visibility = Visibility.Collapsed;
            GestureIDTextInstruction1.Visibility = Visibility.Collapsed;
            GestureIDTextInstruction2.Visibility = Visibility.Collapsed;
            GestureIDTextInstruction3.Visibility = Visibility.Collapsed;
            GestureIDTextInstruction4.Visibility = Visibility.Collapsed;
            GestureIDTextInstruction5.Visibility = Visibility.Collapsed;

            g_RootFolder = ApplicationData.Current.LocalFolder;
            Debug.WriteLine(g_RootFolder.Path);
            g_MediaPlayer = new MediaPlayer();
            g_MediaPlayer.IsLoopingEnabled = true;

            g_CurrentArm = "";

            g_RightExists = false;
            g_LeftExists = false;

            g_FirstTimeIndVar = true;

            JArray g_AnnotatedGesturesID = new JArray();

            g_NodeID = -1;
            g_Nodes = new JArray();
            g_Edges = new JArray();
    }

        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            // Super secure password encryption
            if(UsernameTextbox.Text == "Edkazar" && PasswordTextbox.Password == "jarom217")
            {
                GreetingMessageText.Visibility = Visibility.Collapsed;
                UsernameTextbox.Visibility = Visibility.Collapsed;
                PasswordTextbox.Visibility = Visibility.Collapsed;
                ButtonLogin.Visibility = Visibility.Collapsed;

                GestureIDTextbox.Visibility = Visibility.Visible;
                GestureIDButton.Visibility = Visibility.Visible;
                GestureIDTextInstruction1.Visibility = Visibility.Visible;
                GestureIDTextInstruction2.Visibility = Visibility.Visible;
                GestureIDTextInstruction3.Visibility = Visibility.Visible;
                GestureIDTextInstruction4.Visibility = Visibility.Visible;
                GestureIDTextInstruction5.Visibility = Visibility.Visible;

                loadGestureDatabase();
                loadGesturesIDsFile();
            }
        }

        private void GestureIDButton_Click(object sender, RoutedEventArgs e)
        {
            if (GestureIDTextbox.Text != "")
            {
                ErrorMessageText.Visibility = Visibility.Collapsed;

                JObject selectedGesture = null;

                foreach (JObject obj in g_GestureDatabase)
                {
                    if (obj["Gesture Code"].ToString() == GestureIDTextbox.Text)
                    {
                        selectedGesture = obj;
                        break;
                    }
                }

                if(selectedGesture != null)
                {
                    bool startAnnotating = true;

                    foreach (string ID in g_AnnotatedGesturesID)
                    {
                        if (ID == GestureIDTextbox.Text)
                        {
                            RepeatedGestureMessageText.Visibility = Visibility.Visible;
                            startAnnotating = false;
                            break;
                        }
                    }

                    if (startAnnotating)
                    {
                        RepeatedGestureMessageText.Visibility = Visibility.Collapsed;
                        GestureIDTextbox.Visibility = Visibility.Collapsed;
                        GestureIDButton.Visibility = Visibility.Collapsed;
                        GestureIDTextInstruction1.Visibility = Visibility.Collapsed;
                        GestureIDTextInstruction2.Visibility = Visibility.Collapsed;
                        GestureIDTextInstruction3.Visibility = Visibility.Collapsed;
                        GestureIDTextInstruction4.Visibility = Visibility.Collapsed;
                        GestureIDTextInstruction5.Visibility = Visibility.Collapsed;
                        g_SelectedGestureID = selectedGesture["Gesture Code"].ToString();
                        GestureIDText.Text += g_SelectedGestureID;
                        GestureIDText.Visibility = Visibility.Visible;
                        GestureRecording.Visibility = Visibility.Visible;
                        RightText.Visibility = Visibility.Visible;
                        LeftText.Visibility = Visibility.Visible;
                        SpeechTranscriptionText.Visibility = Visibility.Visible;

                        gestureVideoPlayback(selectedGesture);
                        startGestureAnnotation();
                    }

                }
                else
                {
                    GestureIDTextbox.Visibility = Visibility.Visible;
                    GestureIDButton.Visibility = Visibility.Visible;
                    ErrorMessageText.Visibility = Visibility.Visible;
                }
            }
        }

        private async void loadGestureDatabase()
        {
            string filepath = @"JSONs\GesturesForPaper.json";
            StorageFile file = await g_RootFolder.GetFileAsync(filepath);
            var data = await FileIO.ReadTextAsync(file);
            g_GestureDatabase = JArray.Parse(data);
        }

        private async void gestureVideoPlayback(JObject pSelectedGesture)
        {
            string filepath = @"Videos\";
            filepath += pSelectedGesture["Gesture Code"].ToString()[0];
            filepath += pSelectedGesture["Gesture Code"].ToString()[2];
            filepath += pSelectedGesture["Gesture Code"].ToString()[1] + ".avi";

            StorageFile file = await g_RootFolder.GetFileAsync(filepath);
            GestureRecording.SetMediaPlayer(g_MediaPlayer);
            g_MediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
            g_MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(Convert.ToInt32(pSelectedGesture["Gesture Spotted in Color at"]) - 2);
            g_MediaPlayer.Play();

            SpeechTranscriptionText.Text = pSelectedGesture["Verbal Transcript"].ToString();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(4);
            timer.Start();
            timer.Tick += (o, args) =>
            {
                timer.Start();
                g_MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(Convert.ToInt32(pSelectedGesture["Gesture Spotted in Color at"]) - 2);
            };
        }

        private void startGestureAnnotation()
        {
            TaxonomyAnnotatorGrid.Visibility = Visibility.Visible;
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton thisButton = (ToggleButton)sender;
        }

        private Predicate Create1ArityPredicateAux(string pName, string pArgument1)
        {
            List<string> arguments = new List<string>();
            arguments.Add(pArgument1);
            List<Type> sort = new List<Type>();
            sort.Add(typeof(Variable));
            return g_ESDRSGenerator.createPredicate(pName, sort, arguments);
        }

        private Predicate Create2ArityPredicateAux(string pName, string pArgument1, string pArgument2, bool isTrajectory)
        {
            List<string> arguments = new List<string>();
            arguments.Add(pArgument1);
            arguments.Add(pArgument2);
            List<Type> sort = new List<Type>();
            if (isTrajectory)
            {
                sort.Add(typeof(Mapping));
                sort.Add(typeof(Mapping));
            }
            else
            {
                sort.Add(typeof(Variable));
                sort.Add(typeof(Variable));
            }
            return g_ESDRSGenerator.createPredicate(pName, sort, arguments);
        }

        private Predicate Create3ArityPredicateAux(string pName, string pArgument1, string pArgument2, string pArgument3, bool pIsLoc)
        {
            List<string> arguments = new List<string>();
            arguments.Add(pArgument1);
            arguments.Add(pArgument2);
            arguments.Add(pArgument3);
            List<Type> sort = new List<Type>();
            sort.Add(typeof(Variable));
            sort.Add(typeof(Variable));
            if(pIsLoc)
                sort.Add(typeof(Mapping));
            else
                sort.Add(typeof(Variable));
            return g_ESDRSGenerator.createPredicate(pName, sort, arguments);
        }

        private void TaxButton_Click(object sender, RoutedEventArgs e)
        {
            foreach(Object obj in TaxonomyAnnotatorGrid.Children)
            {
                if (obj.GetType() == typeof(ToggleButton))
                {
                    ToggleButton thisButton = (ToggleButton)obj;
                    if (thisButton.IsChecked == true)
                    {
                        if(thisButton.Content.ToString() != "Gesture")
                        {
                            Predicate newPred = Create1ArityPredicateAux(thisButton.Content.ToString(), ConstantValues.g_GestureString);
                            g_CurrentGesture.TaxClass.Add(newPred);
                        }
                        else
                        {
                            // THE GESTURE ANNOTATION IS CREATED HERE!
                            g_CurrentGesture = g_ESDRSGenerator.createGestureAnnotation(g_SelectedGestureID);
                            Variable newVar = g_ESDRSGenerator.createVariable(ConstantValues.g_GestureString, ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());
                            g_CurrentGesture.Variables.Add(newVar);
                            Predicate newPred = Create1ArityPredicateAux(newVar.name, newVar.name);
                            g_CurrentGesture.ExtraPredicates.Add(newPred);
                        }
                    }
                }
            }
            
            g_CurrentShape = g_ESDRSGenerator.createGestureShape();

            TaxonomyAnnotatorGrid.Visibility = Visibility.Collapsed;
            RightArmText.Visibility = Visibility.Visible;
            YesButton.Visibility = Visibility.Visible;
            NoButton.Visibility = Visibility.Visible;
            g_CurrentArm = ConstantValues.g_RightString;
        }

        private void ShapeCreationButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            if(selectedButton.Content.ToString() == "Yes")
            {
                Variable newVar = new Variable();

                if (g_CurrentArm == ConstantValues.g_RightString)
                {
                    g_RightExists = true;
                    newVar = g_ESDRSGenerator.createVariable(ConstantValues.g_RightString + ConstantValues.g_GestureString, ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());
                }
                else
                {
                    g_LeftExists = true;
                    newVar = g_ESDRSGenerator.createVariable(ConstantValues.g_LeftString + ConstantValues.g_GestureString, ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());      
                }

                g_CurrentGesture.Variables.Add(newVar);
                Predicate newPred = Create1ArityPredicateAux(newVar.name, newVar.name);
                g_CurrentGesture.ExtraPredicates.Add(newPred);

                ArmPoseText.Visibility = Visibility.Visible;
                ExtendedPoseButton.Visibility = Visibility.Visible;
                SemiExtendedPoseButton.Visibility = Visibility.Visible;
                NoExtendedPoseButton.Visibility = Visibility.Visible;

                RightArmText.Visibility = Visibility.Collapsed;
                LeftArmText.Visibility = Visibility.Collapsed;
                YesButton.Visibility = Visibility.Collapsed;
                NoButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                if(g_CurrentArm == ConstantValues.g_RightString)
                {
                    RightArmText.Visibility = Visibility.Collapsed;
                    LeftArmText.Visibility = Visibility.Visible;
                    g_CurrentArm = ConstantValues.g_LeftString;
                }
                else
                {
                    LeftArmText.Visibility = Visibility.Collapsed;
                    YesButton.Visibility = Visibility.Collapsed;
                    NoButton.Visibility = Visibility.Collapsed;
                    AnotherShapeButton.Visibility = Visibility.Visible;
                    ShapeDoneButton.Visibility = Visibility.Visible;
                }
            }
        }

        private void PoseButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            Variable newVar = new Variable();

            if (ArmPoseText.Visibility == Visibility.Visible)
            {
                newVar = g_ESDRSGenerator.createVariable(g_CurrentArm + ConstantValues.g_ArmString, ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());
                g_CurrentShape.Variables.Add(newVar);

                ArmPoseText.Visibility = Visibility.Collapsed;
                ArmOrientationText.Visibility = Visibility.Visible;
            }
            else if (HandPoseText.Visibility == Visibility.Visible)
            {
                newVar = g_ESDRSGenerator.createVariable(g_CurrentArm + ConstantValues.g_HandString, ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());
                g_CurrentShape.Variables.Add(newVar);

                Predicate component = Create2ArityPredicateAux(ConstantValues.g_ComponentString, newVar.name, g_CurrentArm + ConstantValues.g_ArmString, false);
                g_CurrentShape.Component.Add(component);

                HandPoseText.Visibility = Visibility.Collapsed;
                HandOrientationText.Visibility = Visibility.Visible;
            }

            Predicate newPred = Create1ArityPredicateAux(newVar.name, newVar.name);  
            g_CurrentShape.ExtraPredicates.Add(newPred);

            ExtendedPoseButton.Visibility = Visibility.Collapsed;
            SemiExtendedPoseButton.Visibility = Visibility.Collapsed;
            NoExtendedPoseButton.Visibility = Visibility.Collapsed;

            UpOrientationButton.Visibility = Visibility.Visible;
            DownOrientationButton.Visibility = Visibility.Visible;
            LeftOrientationButton.Visibility = Visibility.Visible;
            RightOrientationButton.Visibility = Visibility.Visible;
            ForwardOrientationButton.Visibility = Visibility.Visible;
        }

        private void OrientationButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            Predicate newPred = new Predicate();

            if (ArmOrientationText.Visibility == Visibility.Visible)
            {
                newPred = Create1ArityPredicateAux(ConstantValues.g_OrientationString + selectedButton.Content.ToString(), g_CurrentArm + ConstantValues.g_ArmString);

                ArmOrientationText.Visibility = Visibility.Collapsed;
                HandPoseText.Visibility = Visibility.Visible;
                ExtendedPoseButton.Visibility = Visibility.Visible;
                SemiExtendedPoseButton.Visibility = Visibility.Visible;
                NoExtendedPoseButton.Visibility = Visibility.Visible;
            }
            else if (HandOrientationText.Visibility == Visibility.Visible)
            {
                newPred = Create1ArityPredicateAux(ConstantValues.g_OrientationString + selectedButton.Content.ToString(), g_CurrentArm + ConstantValues.g_HandString);

                HandOrientationText.Visibility = Visibility.Collapsed;
                LittleFingerText.Visibility = Visibility.Visible;
                FingerDescriptionText.Visibility = Visibility.Visible;
                FingerExtendedPoseButton.Visibility = Visibility.Visible;
                FingerSemiExtendedPoseButton.Visibility = Visibility.Visible;
                FingerNoExtendedPoseButton.Visibility = Visibility.Visible;
            }

            g_CurrentShape.Orientation.Add(newPred);

            UpOrientationButton.Visibility = Visibility.Collapsed;
            DownOrientationButton.Visibility = Visibility.Collapsed;
            LeftOrientationButton.Visibility = Visibility.Collapsed;
            RightOrientationButton.Visibility = Visibility.Collapsed;
            ForwardOrientationButton.Visibility = Visibility.Collapsed;
        }

        private void FingerPoseButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            Variable newVar = new Variable();
            string varName = g_CurrentArm;

            if (LittleFingerText.Visibility == Visibility.Visible)
            {
                varName = varName + LittleFingerText.Text + ConstantValues.g_FingerString;

                LittleFingerText.Visibility = Visibility.Collapsed;
                RingFingerText.Visibility = Visibility.Visible;
            }
            else if (RingFingerText.Visibility == Visibility.Visible)
            {
                varName = varName + RingFingerText.Text + ConstantValues.g_FingerString;

                RingFingerText.Visibility = Visibility.Collapsed;
                MiddleFingerText.Visibility = Visibility.Visible;
            }
            else if (MiddleFingerText.Visibility == Visibility.Visible)
            {
                varName = varName + MiddleFingerText.Text + ConstantValues.g_FingerString;

                MiddleFingerText.Visibility = Visibility.Collapsed;
                IndexFingerText.Visibility = Visibility.Visible;
            }
            else if (IndexFingerText.Visibility == Visibility.Visible)
            {
                varName = varName + IndexFingerText.Text + ConstantValues.g_FingerString;

                IndexFingerText.Visibility = Visibility.Collapsed;
                ThumbFingerText.Visibility = Visibility.Visible;
            }
            else
            {
                varName = varName + ThumbFingerText.Text + ConstantValues.g_FingerString;

                ThumbFingerText.Visibility = Visibility.Collapsed;
                LittleFingerText.Visibility = Visibility.Visible;

                FingerDescriptionText.Visibility = Visibility.Collapsed;
                FingerOrientationText.Visibility = Visibility.Visible;
                FingerExtendedPoseButton.Visibility = Visibility.Collapsed;
                FingerSemiExtendedPoseButton.Visibility = Visibility.Collapsed;
                FingerNoExtendedPoseButton.Visibility = Visibility.Collapsed;

                FingerNoOrientationButton.Visibility = Visibility.Visible;
                FingerUpOrientationButton.Visibility = Visibility.Visible;
                FingerDownOrientationButton.Visibility = Visibility.Visible;
                FingerLeftOrientationButton.Visibility = Visibility.Visible;
                FingerRightOrientationButton.Visibility = Visibility.Visible;
                FingerForwardOrientationButton.Visibility = Visibility.Visible;
            }

            newVar = g_ESDRSGenerator.createVariable(varName, ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());
            g_CurrentShape.Variables.Add(newVar);

            Predicate component = Create2ArityPredicateAux(ConstantValues.g_ComponentString, newVar.name, g_CurrentArm + ConstantValues.g_HandString, false);
            g_CurrentShape.Component.Add(component);

            Predicate newPred = Create1ArityPredicateAux(ConstantValues.g_PoseString + selectedButton.Content.ToString(), newVar.name);
            g_CurrentShape.Pose.Add(newPred);
        }

        private void FingerOrientationButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            string predName = g_CurrentArm;

            if (LittleFingerText.Visibility == Visibility.Visible)
            {
                predName = predName + LittleFingerText.Text + ConstantValues.g_FingerString;

                LittleFingerText.Visibility = Visibility.Collapsed;
                RingFingerText.Visibility = Visibility.Visible;
            }
            else if (RingFingerText.Visibility == Visibility.Visible)
            {
                predName = predName + RingFingerText.Text + ConstantValues.g_FingerString;

                RingFingerText.Visibility = Visibility.Collapsed;
                MiddleFingerText.Visibility = Visibility.Visible;
            }
            else if (MiddleFingerText.Visibility == Visibility.Visible)
            {
                predName = predName + MiddleFingerText.Text + ConstantValues.g_FingerString;

                MiddleFingerText.Visibility = Visibility.Collapsed;
                IndexFingerText.Visibility = Visibility.Visible;
            }
            else if (IndexFingerText.Visibility == Visibility.Visible)
            {
                predName = predName + IndexFingerText.Text + ConstantValues.g_FingerString;

                IndexFingerText.Visibility = Visibility.Collapsed;
                ThumbFingerText.Visibility = Visibility.Visible;
            }
            else
            {
                predName = predName + ThumbFingerText.Text + ConstantValues.g_FingerString;

                LittleFingerText.Visibility = Visibility.Visible;
                RingFingerText.Visibility = Visibility.Visible;
                MiddleFingerText.Visibility = Visibility.Visible;
                IndexFingerText.Visibility = Visibility.Visible;

                FingerOrientationText.Visibility = Visibility.Collapsed;
                FingerSeparationText.Visibility = Visibility.Visible;
                FingerNoOrientationButton.Visibility = Visibility.Collapsed;
                FingerUpOrientationButton.Visibility = Visibility.Collapsed;
                FingerDownOrientationButton.Visibility = Visibility.Collapsed;
                FingerLeftOrientationButton.Visibility = Visibility.Collapsed;
                FingerRightOrientationButton.Visibility = Visibility.Collapsed;
                FingerForwardOrientationButton.Visibility = Visibility.Collapsed;

                LittleFingerSwitch.Visibility = Visibility.Visible;
                RingFingerSwitch.Visibility = Visibility.Visible;
                MiddleFingerSwitch.Visibility = Visibility.Visible;
                IndexFingerSwitch.Visibility = Visibility.Visible;

                DoneSeparatingButton.Visibility = Visibility.Visible;
            }

            Predicate newPred = Create1ArityPredicateAux(ConstantValues.g_OrientationString + selectedButton.Content.ToString(), predName);
            g_CurrentShape.Orientation.Add(newPred);
        }

        private void DoneWithFingersButton_Click(object sender, RoutedEventArgs e)
        {
            createPredforSwitches();

            if (g_CurrentArm == ConstantValues.g_RightString)
            {
                LeftArmText.Visibility = Visibility.Visible;
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                g_CurrentArm = ConstantValues.g_LeftString;
            }
            else
            {
                LeftArmText.Visibility = Visibility.Collapsed;
                YesButton.Visibility = Visibility.Collapsed;
                NoButton.Visibility = Visibility.Collapsed;

                if (g_RightExists && g_LeftExists)
                {
                    isDependentText.Visibility = Visibility.Visible;
                    YesDependentButton.Visibility = Visibility.Visible;
                    NoDependentButton.Visibility = Visibility.Visible;
                }
                else
                {
                    AnotherShapeButton.Visibility = Visibility.Visible;
                    ShapeDoneButton.Visibility = Visibility.Visible;
                }
            }

            FingerSeparationText.Visibility = Visibility.Collapsed;

            LittleFingerText.Visibility = Visibility.Collapsed;
            RingFingerText.Visibility = Visibility.Collapsed;
            MiddleFingerText.Visibility = Visibility.Collapsed;
            IndexFingerText.Visibility = Visibility.Collapsed;
            ThumbFingerText.Visibility = Visibility.Collapsed;

            LittleFingerSwitch.IsOn = false;
            RingFingerSwitch.IsOn = false;
            MiddleFingerSwitch.IsOn = false;
            IndexFingerSwitch.IsOn = false;
            LittleFingerSwitch.Visibility = Visibility.Collapsed;
            RingFingerSwitch.Visibility = Visibility.Collapsed;
            MiddleFingerSwitch.Visibility = Visibility.Collapsed;
            IndexFingerSwitch.Visibility = Visibility.Collapsed;

            DoneSeparatingButton.Visibility = Visibility.Collapsed;
        }

        private void createPredforSwitches()
        {
            if (LittleFingerSwitch.IsOn)
            {
                Predicate newPred = Create2ArityPredicateAux(ConstantValues.g_FingerString + ConstantValues.g_SeparatedString,
                    g_CurrentArm + LittleFingerText.Text + ConstantValues.g_FingerString,
                    g_CurrentArm + RingFingerText.Text + ConstantValues.g_FingerString, false);
                g_CurrentShape.Separation.Add(newPred);
            }

            if (RingFingerSwitch.IsOn)
            {
                Predicate newPred = Create2ArityPredicateAux(ConstantValues.g_FingerString + ConstantValues.g_SeparatedString,
                    g_CurrentArm + RingFingerText.Text + ConstantValues.g_FingerString,
                    g_CurrentArm + MiddleFingerText.Text + ConstantValues.g_FingerString, false);
                g_CurrentShape.Separation.Add(newPred);
            }

            if (MiddleFingerSwitch.IsOn)
            {
                Predicate newPred = Create2ArityPredicateAux(ConstantValues.g_FingerString + ConstantValues.g_SeparatedString,
                    g_CurrentArm + MiddleFingerText.Text + ConstantValues.g_FingerString,
                    g_CurrentArm + IndexFingerText.Text + ConstantValues.g_FingerString, false);
                g_CurrentShape.Separation.Add(newPred);
            }

            if (IndexFingerSwitch.IsOn)
            {
                Predicate newPred = Create2ArityPredicateAux(ConstantValues.g_FingerString + ConstantValues.g_SeparatedString,
                    g_CurrentArm + IndexFingerText.Text + ConstantValues.g_FingerString,
                    g_CurrentArm + ThumbFingerText.Text + ConstantValues.g_FingerString, false);
                g_CurrentShape.Separation.Add(newPred);
            }
        }

        private void OptionDependentButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;

            if (selectedButton.Content.ToString() == "Yes")
            {
                Predicate newPred = Create2ArityPredicateAux(ConstantValues.g_DependenceString,
                    ConstantValues.g_RightString + ConstantValues.g_GestureString, ConstantValues.g_LeftString + ConstantValues.g_GestureString, false);
                g_CurrentShape.Dependence = newPred;
            }

            

            isDependentText.Visibility = Visibility.Collapsed;
            YesDependentButton.Visibility = Visibility.Collapsed;
            NoDependentButton.Visibility = Visibility.Collapsed;

            AnotherShapeButton.Visibility = Visibility.Visible;
            ShapeDoneButton.Visibility = Visibility.Visible;
        }

        private void AnotherShapeButton_Click(object sender, RoutedEventArgs e)
        {
            RightArmText.Visibility = Visibility.Visible;
            YesButton.Visibility = Visibility.Visible;
            NoButton.Visibility = Visibility.Visible;
            g_CurrentArm = ConstantValues.g_RightString;

            g_CurrentGesture.Shape.Add(g_CurrentShape);
            g_CurrentShape = g_ESDRSGenerator.createGestureShape();

            AnotherShapeButton.Visibility = Visibility.Collapsed;
            ShapeDoneButton.Visibility = Visibility.Collapsed;
        }

        private void ShapeDoneButton_Click(object sender, RoutedEventArgs e)
        {
            AnotherShapeButton.Visibility = Visibility.Collapsed;
            ShapeDoneButton.Visibility = Visibility.Collapsed;

            g_CurrentGesture.Shape.Add(g_CurrentShape);
            g_CurrentShape = g_ESDRSGenerator.createGestureShape();

            g_CurrentMovement = g_ESDRSGenerator.createGestureMovement();
            
            if (g_RightExists)
            {
                RightPlaceholdersText.Visibility = Visibility.Visible;
                NumPlaceholdersTextbox.Visibility = Visibility.Visible;
                DoneNumPlaceholdersButton.Visibility = Visibility.Visible;
                g_CurrentArm = ConstantValues.g_RightString;
            }
            else if (g_LeftExists)
            {
                LeftPlaceholdersText.Visibility = Visibility.Visible;
                NumPlaceholdersTextbox.Visibility = Visibility.Visible;
                DoneNumPlaceholdersButton.Visibility = Visibility.Visible;
                g_CurrentArm = ConstantValues.g_LeftString;
            }
            else
            {
                DoneTrajectoryButton.Visibility = Visibility.Visible;
            }
        }

        private void DoneNumPlaceholdersButton_Click(object sender, RoutedEventArgs e)
        {
            if(NumPlaceholdersTextbox.Text != "")
            {
                ErrorTrajectoryMessageText.Visibility = Visibility.Collapsed;

                g_NumPlaceholders = Int32.Parse(NumPlaceholdersTextbox.Text);
                g_CurrentPlaceholder = 1;
                bool firstPoint = true;

                for (int counter = 0; counter < g_NumPlaceholders - 1; counter++)
                {
                    //Assuming we are getting the Spationtemporal info. I neeed to do this.

                    if (firstPoint)
                    {
                        string firstPointName = g_CurrentArm + ConstantValues.g_PlaceholderPointString + (counter + 1).ToString();
                        Spatiotemporal firstPlaceholderPoint = g_ESDRSGenerator.createSpatiotemporal(firstPointName, 0, 0, 0, new TimeSpan());
                        g_CurrentMovement.Points.Add(firstPlaceholderPoint);
                        Mapping firstNewMapping = g_ESDRSGenerator.createMapping(firstPointName, new Matrix3D(), firstPointName);
                        g_CurrentGesture.Mappings.Add(firstNewMapping);
                        firstPoint = false;
                    }

                    string pointName = g_CurrentArm + ConstantValues.g_PlaceholderPointString + (counter + 2).ToString();
                    Spatiotemporal newPlaceholderPoint = g_ESDRSGenerator.createSpatiotemporal(pointName, 0, 0, 0, new TimeSpan());
                    g_CurrentMovement.Points.Add(newPlaceholderPoint);
                    Mapping newMapping = g_ESDRSGenerator.createMapping(pointName, new Matrix3D(), pointName);
                    g_CurrentGesture.Mappings.Add(newMapping);

                    Variable newTraj = g_ESDRSGenerator.createVariable(g_CurrentArm + ConstantValues.g_TrajectoryString + (counter + 1).ToString(), ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());
                    g_CurrentMovement.Variables.Add(newTraj);
                    Predicate newPred = Create2ArityPredicateAux(ConstantValues.g_TrajectoryString, g_CurrentMovement.Points[counter].name, g_CurrentMovement.Points[counter + 1].name, true);
                    g_CurrentMovement.Trajectories.Add(newPred);
                }

                LeftPlaceholdersText.Visibility = Visibility.Collapsed;
                RightPlaceholdersText.Visibility = Visibility.Collapsed;
                NumPlaceholdersTextbox.Visibility = Visibility.Collapsed;
                DoneNumPlaceholdersButton.Visibility = Visibility.Collapsed;

                MotionPlanesImage.Visibility = Visibility.Visible;
                DescribePlaneOfMotion.Visibility = Visibility.Visible;
                MotionCoronalButton.Visibility = Visibility.Visible;
                MotionSagittalButton.Visibility = Visibility.Visible;
                MotionTransverseButton.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorTrajectoryMessageText.Visibility = Visibility.Visible;
            }
        }

        private void MotionOptionButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;

            Predicate newPred = Create1ArityPredicateAux(ConstantValues.g_MainPlaneString + selectedButton.Content.ToString(), g_CurrentArm + ConstantValues.g_GestureString);
            g_CurrentMovement.MainPlane = newPred;

            DescribePlaneOfMotion.Visibility = Visibility.Collapsed;
            MotionCoronalButton.Visibility = Visibility.Collapsed;
            MotionSagittalButton.Visibility = Visibility.Collapsed;
            MotionTransverseButton.Visibility = Visibility.Collapsed;

            char[] chars = DescribeTrajectoryText.Text.ToCharArray();
            chars[chars.Length - 1] = Convert.ToChar(g_CurrentPlaceholder.ToString());
            DescribeTrajectoryText.Text = new string(chars);
            DescribeTrajectoryText.Visibility = Visibility.Visible;
            MotionDirectionsGrid.Visibility = Visibility.Visible;
        }    

        private void DoneMotioningOptionButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (Object obj in MotionDirectionsGrid.Children)
            {
                if (obj.GetType() == typeof(ToggleButton))
                {
                    ToggleButton thisButton = (ToggleButton)obj;
                    if (thisButton.IsChecked == true)
                    {
                        // create
                        Predicate newPred = Create1ArityPredicateAux(ConstantValues.g_DirectionString + thisButton.Content, g_CurrentMovement.Variables[g_CurrentPlaceholder - 1].name);
                        g_CurrentMovement.Directions.Add(newPred);                        
                    }

                    thisButton.IsChecked = false;
                }
            }
            
            g_CurrentPlaceholder++;

            if (g_CurrentPlaceholder<g_NumPlaceholders)
            {
                char[] chars = DescribeTrajectoryText.Text.ToCharArray();
                chars[chars.Length-1] = Convert.ToChar(g_CurrentPlaceholder.ToString());
                DescribeTrajectoryText.Text = new string(chars);
            }
            else
            {
                DescribeTrajectoryText.Visibility = Visibility.Collapsed;
                MotionDirectionsGrid.Visibility = Visibility.Collapsed;
                MotionPlanesImage.Visibility = Visibility.Collapsed;

                if (g_CurrentArm == ConstantValues.g_RightString && g_LeftExists)
                {
                    NumPlaceholdersTextbox.Text = "";
                    LeftPlaceholdersText.Visibility = Visibility.Visible;
                    NumPlaceholdersTextbox.Visibility = Visibility.Visible;
                    DoneNumPlaceholdersButton.Visibility = Visibility.Visible;
                    g_CurrentArm = ConstantValues.g_LeftString;
                }
                else
                {
                    DoneTrajectoryButton.Visibility = Visibility.Visible;
                }
            }           
        }

        private void DoneTrajectoringButton_Click(object sender, RoutedEventArgs e)
        {
            g_CurrentGesture.Movement = g_CurrentMovement;
            g_CurrentMovement = g_ESDRSGenerator.createGestureMovement();

            DoneTrajectoryButton.Visibility = Visibility.Collapsed;

            if (g_RightExists || g_LeftExists)
            {
                doesExemplifyText.Visibility = Visibility.Visible;
                YesExemplifiesoButton.Visibility = Visibility.Visible;
                NoExemplifiesButton.Visibility = Visibility.Visible;
            }
            else
            {
                DoneExemplifyingButton.Visibility = Visibility.Visible;
            }
        }

        private void OptionExemplifiesButton_OnClick(object sender, RoutedEventArgs e)
        {
            doesExemplifyText.Visibility = Visibility.Collapsed;
            YesExemplifiesoButton.Visibility = Visibility.Collapsed;
            NoExemplifiesButton.Visibility = Visibility.Collapsed;

            Button selectedButton = sender as Button;

            if (selectedButton.Content.ToString() == "Yes")
            {
                if (g_RightExists)
                {
                    ExemplifiesGrid.Visibility = Visibility.Visible;
                    RightExemplefiesText.Visibility = Visibility.Visible;
                    g_CurrentArm = ConstantValues.g_RightString;
                }
                else
                {
                    ExemplifiesGrid.Visibility = Visibility.Visible;
                    LeftExemplefiesText.Visibility = Visibility.Visible;
                    g_CurrentArm = ConstantValues.g_LeftString;
                }
            }
            else
            {
                DoneExemplifyingButton.Visibility = Visibility.Visible;
            }
        }

        private void CreateIndVarButton_Click(object sender, RoutedEventArgs e)
        {
            if (IndividualTextbox.Text != "")
            {
                Variable newVar = g_ESDRSGenerator.createVariable(IndividualTextbox.Text, ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());
                g_CurrentGesture.ContextVars.Add(newVar);
                Predicate newPred = Create1ArityPredicateAux(newVar.name, newVar.name);
                g_CurrentGesture.ContextPreds.Add(newPred);

                IndVarComboBox.Items.Add(IndividualTextbox.Text);
                IndVarComboBox2.Items.Add(IndividualTextbox.Text);
                IndividualTextbox.Text = "";
            }   
        }

        private void DoneAssigningVarButton_Click(object sender, RoutedEventArgs e)
        {
            if (IndVarComboBox.SelectedItem != null)
            {
                Predicate newPred = Create2ArityPredicateAux(ConstantValues.g_ExemplifiesString, g_CurrentArm + ConstantValues.g_GestureString, IndVarComboBox.SelectedItem.ToString(), false);
                g_CurrentGesture.Exemplifies.Add(newPred);

                if (g_CurrentArm == ConstantValues.g_RightString)
                {
                    RightExemplefiesText.Visibility = Visibility.Collapsed;

                    if (g_LeftExists)
                    {
                        LeftExemplefiesText.Visibility = Visibility.Visible;
                        g_CurrentArm = ConstantValues.g_LeftString;
                    }
                    else
                    {
                        ExemplifiesGrid.Visibility = Visibility.Collapsed;
                        g_CurrentArm = ConstantValues.g_RightString;

                        DoneExemplifyingButton.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    LeftExemplefiesText.Visibility = Visibility.Collapsed;
                    ExemplifiesGrid.Visibility = Visibility.Collapsed;

                    DoneExemplifyingButton.Visibility = Visibility.Visible;
                }

                IndVarComboBox.SelectedItem = null;
            }
        }

        private void DoneExemplifyingButton_Click(object sender, RoutedEventArgs e)
        {
            DoneExemplifyingButton.Visibility = Visibility.Collapsed;

            if (g_RightExists || g_LeftExists)
            {
                isSynchroText.Visibility = Visibility.Visible;
                YesSynchroButton.Visibility = Visibility.Visible;
                NoSynchroButton.Visibility = Visibility.Visible;
            }
            else
            {
                DoneSynchronizingButton.Visibility = Visibility.Visible;
            }
        }

        private void OptionSynchroButton_OnClick(object sender, RoutedEventArgs e)
        {
            isSynchroText.Visibility = Visibility.Collapsed;
            YesSynchroButton.Visibility = Visibility.Collapsed;
            NoSynchroButton.Visibility = Visibility.Collapsed;

            Button selectedButton = sender as Button;

            if (selectedButton.Content.ToString() == "Yes")
            {
                ChoooseEventText.Visibility = Visibility.Visible;
                EventVarComboBox.Visibility = Visibility.Visible;
                CreatedEventVar.Visibility = Visibility.Visible;
                CreateIndVarText2.Visibility = Visibility.Visible;
                EventualityTextbox.Visibility = Visibility.Visible;
                CreateEventVarButton.Visibility = Visibility.Visible;
            }
            else
            {
                DoneSynchronizingButton.Visibility = Visibility.Visible;
            }
        }

        private void CreateEventVarButton_Click(object sender, RoutedEventArgs e)
        {
            if (EventualityTextbox.Text != "")
            {
                selectSynchroEventText.Visibility = Visibility.Visible;
                EventualityTextbox.Visibility = Visibility.Visible;
                CreateIndVarButton2.Visibility = Visibility.Visible;
                IndVarComboBox2.Visibility = Visibility.Visible;
                IndividualTextbox2.Visibility = Visibility.Visible;
                AddIndVarText.Visibility = Visibility.Visible;
                AddIndtoEventButton.Visibility = Visibility.Visible;
                DoneEventVarButton.Visibility = Visibility.Visible;

                CreatedEventVar.Text = EventualityTextbox.Text + "()";
            }
        }

        private void CreateIndVarButton2_Click(object sender, RoutedEventArgs e)
        {
            if (IndividualTextbox2.Text != "")
            {
                Variable newVar = g_ESDRSGenerator.createVariable(IndividualTextbox2.Text, ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());
                g_CurrentGesture.ContextVars.Add(newVar);
                Predicate newPred = Create1ArityPredicateAux(newVar.name, newVar.name);
                g_CurrentGesture.ContextPreds.Add(newPred);

                IndVarComboBox.Items.Add(IndividualTextbox2.Text);
                IndVarComboBox2.Items.Add(IndividualTextbox2.Text);
                IndividualTextbox2.Text = "";   
            }
        }

        private void AddIndtoEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (IndVarComboBox2.SelectedItem != null)
            {
                CreatedEventVar.Text = CreatedEventVar.Text.Remove(CreatedEventVar.Text.Length - 1);
                if (!g_FirstTimeIndVar)
                {
                    CreatedEventVar.Text += ",";
                }

                g_FirstTimeIndVar = false;
                CreatedEventVar.Text = CreatedEventVar.Text + IndVarComboBox2.SelectedValue + ")";
                IndVarComboBox2.SelectedItem = null;
            }
        }

        private void DoneEventVarButton_Click(object sender, RoutedEventArgs e)
        {
            if(EventualityTextbox.Text != "")
            {
                // NEED TO DEFINE TIMES
                Variable newVar = g_ESDRSGenerator.createVariable(EventualityTextbox.Text, ConstantValues.g_EventualityString, new TimeSpan(), new TimeSpan());
                g_CurrentGesture.ContextVars.Add(newVar);

                EventVarComboBox.Items.Add(EventualityTextbox.Text);
                IndVarComboBox2.SelectedItem = null;
                g_FirstTimeIndVar = true;
                CreatedEventVar.Text = "";
                EventualityTextbox.Text = "";

                selectSynchroEventText.Visibility = Visibility.Collapsed;
                IndividualTextbox2.Visibility = Visibility.Collapsed;
                CreateIndVarButton2.Visibility = Visibility.Collapsed;
                IndVarComboBox2.Visibility = Visibility.Collapsed;
                AddIndVarText.Visibility = Visibility.Collapsed;
                AddIndtoEventButton.Visibility = Visibility.Collapsed;
                DoneEventVarButton.Visibility = Visibility.Collapsed;

                FinishEventVarButton.Visibility = Visibility.Visible;
            }
        }

        private void FinishEventVarButton_Click(object sender, RoutedEventArgs e)
        {
            if (EventVarComboBox.SelectedItem != null)
            {
                Predicate newPred = Create2ArityPredicateAux(ConstantValues.g_SynchroString, EventVarComboBox.SelectedItem.ToString(), ConstantValues.g_GestureString, false);
                g_CurrentGesture.Synchro = newPred;

                EventVarComboBox.SelectedItem = null;

                ChoooseEventText.Visibility = Visibility.Collapsed;
                EventVarComboBox.Visibility = Visibility.Collapsed;
                CreatedEventVar.Visibility = Visibility.Collapsed;
                CreateIndVarText2.Visibility = Visibility.Collapsed;
                EventualityTextbox.Visibility = Visibility.Collapsed;
                CreateEventVarButton.Visibility = Visibility.Collapsed;
                FinishEventVarButton.Visibility = Visibility.Collapsed;

                DoneSynchronizingButton.Visibility = Visibility.Visible;
            }
        }

        private void DoneSynchronizingButton_Click(object sender, RoutedEventArgs e)
        {
            if (g_RightExists || g_LeftExists)
            {
                YesLocButton.Visibility = Visibility.Visible;
                NoLocButton.Visibility = Visibility.Visible;

                DoneSynchronizingButton.Visibility = Visibility.Collapsed;

                if (g_RightExists)
                {
                    isRightLocText.Visibility = Visibility.Visible;
                    g_CurrentArm = ConstantValues.g_RightString;
                }
                else
                {
                    isLeftLocText.Visibility = Visibility.Visible;
                    g_CurrentArm = ConstantValues.g_LeftString;
                }
            }
            else
            {
                DoneSynchronizingButton.Visibility = Visibility.Collapsed;

                DoneLocalizingButton.Visibility = Visibility.Visible;
            }
        }

        private void OptionLocButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            if (selectedButton.Content.ToString() == "Yes")
            {
                isRightLocText.Visibility = Visibility.Collapsed;
                isLeftLocText.Visibility = Visibility.Collapsed;
                YesLocButton.Visibility = Visibility.Collapsed;
                NoLocButton.Visibility = Visibility.Collapsed;

                EventVarComboBox.Visibility = Visibility.Visible;
                IndVarComboBox2.Visibility = Visibility.Visible;
                SelectPhysicalLocText.Visibility = Visibility.Visible;
                PhysicalLocTextbox.Visibility = Visibility.Visible;
                PhysicalLocComboBox.Visibility = Visibility.Visible;
                SelectIndividualText.Visibility = Visibility.Visible;
                CreateLocButton.Visibility = Visibility.Visible;
                AssignLocButton.Visibility = Visibility.Visible;

                if(g_CurrentArm == ConstantValues.g_RightString)
                {
                    LocforRightText.Visibility = Visibility.Visible;
                }
                else
                {
                    LocforLeftText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                LocforRightText.Visibility = Visibility.Collapsed;
                LocforLeftText.Visibility = Visibility.Collapsed;
                isRightLocText.Visibility = Visibility.Collapsed;
                isLeftLocText.Visibility = Visibility.Collapsed;
                YesLocButton.Visibility = Visibility.Collapsed;
                NoLocButton.Visibility = Visibility.Collapsed;

                if (g_CurrentArm == ConstantValues.g_RightString)
                {
                    LocforLeftText.Visibility = Visibility.Visible;
                    YesLocButton.Visibility = Visibility.Visible;
                    NoLocButton.Visibility = Visibility.Visible;

                    g_CurrentArm = ConstantValues.g_LeftString;
                }
                else
                {
                    DoneLocalizingButton.Visibility = Visibility.Visible;
                }
            }
        }

        private void CreateLocButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(PhysicalLocTextbox.Text != "")
            {
                // Add coordinates and time
                Spatiotemporal newPlaceholderPoint = g_ESDRSGenerator.createSpatiotemporal(PhysicalLocTextbox.Text, 0, 0, 0, new TimeSpan());
                g_CurrentGesture.Spatiotemporals.Add(newPlaceholderPoint);
                Mapping newMapping = g_ESDRSGenerator.createMapping(PhysicalLocTextbox.Text, new Matrix3D(), PhysicalLocTextbox.Text);
                g_CurrentGesture.Mappings.Add(newMapping);

                PhysicalLocComboBox.Items.Add(PhysicalLocTextbox.Text);

                PhysicalLocTextbox.Text = "";
            }
        }

        private void AssignLocButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (EventVarComboBox.SelectedItem != null && IndVarComboBox2.SelectedItem != null && PhysicalLocComboBox.SelectedItem != null)
            {

                Predicate newLoc = Create3ArityPredicateAux(ConstantValues.g_LocString, EventVarComboBox.SelectedItem.ToString(),
                    g_CurrentArm + ConstantValues.g_GestureString, PhysicalLocComboBox.SelectedItem.ToString(), true);
                g_CurrentGesture.Loc.Add(newLoc);

                EventVarComboBox.SelectedItem = null;
                IndVarComboBox2.SelectedItem = null;
                PhysicalLocComboBox.SelectedItem = null;

                EventVarComboBox.Visibility = Visibility.Collapsed;
                IndVarComboBox2.Visibility = Visibility.Collapsed;
                SelectPhysicalLocText.Visibility = Visibility.Collapsed;
                PhysicalLocTextbox.Visibility = Visibility.Collapsed;
                SelectIndividualText.Visibility = Visibility.Collapsed;
                PhysicalLocComboBox.Visibility = Visibility.Collapsed;
                CreateLocButton.Visibility = Visibility.Collapsed;
                AssignLocButton.Visibility = Visibility.Collapsed;
                LocforRightText.Visibility = Visibility.Collapsed;
                LocforLeftText.Visibility = Visibility.Collapsed;

                if (g_CurrentArm == ConstantValues.g_RightString)
                {
                    if (g_LeftExists)
                    {
                        g_CurrentArm = ConstantValues.g_LeftString;

                        YesLocButton.Visibility = Visibility.Visible;
                        NoLocButton.Visibility = Visibility.Visible;
                        isLeftLocText.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        DoneLocalizingButton.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    DoneLocalizingButton.Visibility = Visibility.Visible;
                }
            }
        }

        private void DoneLocalizingButton_Click(object sender, RoutedEventArgs e)
        {
            DoneLocalizingButton.Visibility = Visibility.Collapsed;

            DoneAnnotatingButton.Visibility = Visibility.Visible;
        }

        private void DoneAnnotatingButton_Click(object sender, RoutedEventArgs e)
        {
            DoneAnnotatingButton.Visibility = Visibility.Collapsed;

            GestureIDTextbox.Visibility = Visibility.Visible;
            GestureIDButton.Visibility = Visibility.Visible;
            GestureIDTextInstruction1.Visibility = Visibility.Visible;
            GestureIDTextInstruction2.Visibility = Visibility.Visible;
            GestureIDTextInstruction3.Visibility = Visibility.Visible;
            GestureIDTextInstruction4.Visibility = Visibility.Visible;
            GestureIDTextInstruction5.Visibility = Visibility.Visible;
            QuitButton.Visibility = Visibility.Visible;

            // Constructs a JSON from the current gesture annotation
            //JObject toSave = constructionJSONfromAnnotation();

            // Saves the created JSON to a file
            //saveCurrentGestureAnnotation(g_SelectedGestureID, toSave);

            // Constructs a JSON in NetworkX format
            JObject toSave2 = constructionNetworkXJSON();

            // Saves the created JSON to a file
            saveCurrentGestureAnnotation(g_SelectedGestureID+"NetworkX", toSave2);

            // Saves the selected gesture ID to the file of annotated gestures
            saveGestureIDtoFile();
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            CoreApplication.Exit();
        }

        private async void loadGesturesIDsFile()
        {
            string filepath = @"JSONs\gesturesIDs.json";
            StorageFile file = await g_RootFolder.CreateFileAsync(filepath, CreationCollisionOption.OpenIfExists);
            var data = await FileIO.ReadTextAsync(file);

            g_AnnotatedGesturesID = new JArray();
            if (data.Length > 0)
            {
                g_AnnotatedGesturesID = JArray.Parse(data);
            }
        }

        private async void saveGestureIDtoFile()
        {
            //save gesture id to annotated id file
            string filepath = @"JSONs\gesturesIDs.json";
            StorageFile file = await g_RootFolder.CreateFileAsync(filepath, CreationCollisionOption.OpenIfExists);
            g_AnnotatedGesturesID.Add(g_SelectedGestureID);
            string string_to_send = JsonConvert.SerializeObject(g_AnnotatedGesturesID, Formatting.None);
            await FileIO.WriteTextAsync(file, string_to_send);
        }

        private JObject constructionJSONfromAnnotation()
        {
            JObject GestureAnnotation = new JObject();
            JObject GestureContext = new JObject();
            JArray ContextPredicates = new JArray();
            JObject GesturePredicates = new JObject();
            JArray TaxClassPredicates = new JArray();
            JArray GestureShapes = new JArray();
            JObject GestureMovement = new JObject();
            JArray ExemplifiesPredicates = new JArray();
            JObject SynchroPredicate = new JObject();
            JArray LocPredicates = new JArray();
            JArray ExtraPredicates = new JArray();

            // -------------- Main Gesture -------------- //
            GestureAnnotation["Name"] = g_CurrentGesture.name; // Adding Gesture Name
            assembleJSONVariables(g_CurrentGesture.Variables, GestureAnnotation); // Adding Gesture Variables
            assembleJSONSpatiotemporal(g_CurrentGesture.Spatiotemporals, GestureAnnotation);  // Adding Gesture Spatiotemporals
            assembleJSONMapping(g_CurrentGesture.Mappings, GestureAnnotation);  // Adding Gesture Mappings

            // -------------- Gesture Context -------------- //
            assembleJSONVariables(g_CurrentGesture.ContextVars, GestureContext); // Adding Context Variables
            assembleJSONPredicateList(g_CurrentGesture.ContextPreds, ContextPredicates); // Adding Context Predicates
            GestureContext.Add("Predicates", ContextPredicates);
            GestureAnnotation["Context"] = GestureContext; // Adding Context

            // -------------- Gesture Predicates -------------- //
            // -------------- Gesture Taxonomy -------------- //
            assembleJSONPredicateList(g_CurrentGesture.TaxClass, TaxClassPredicates); // Adding Taxonomy Predicates
            GesturePredicates.Add("Taxonomies", TaxClassPredicates);

            // -------------- Gesture Shape -------------- //
            assembleJSONShapeList(g_CurrentGesture.Shape, GestureShapes); // Adding Shape (see function)
            GesturePredicates.Add("Shapes", GestureShapes);

            // -------------- Gesture Movement -------------- //
            assembleJSONMovement(g_CurrentGesture.Movement, GestureMovement); // Adding Movement (see function)
            GesturePredicates["Movement"] = GestureMovement;

            // -------------- Gesture Exemplifies -------------- //
            assembleJSONPredicateList(g_CurrentGesture.Exemplifies, ExemplifiesPredicates); // Adding Exemplify Predicates
            GesturePredicates.Add("Exemplifies", ExemplifiesPredicates);

            // -------------- Gesture Synchro -------------- //
            GesturePredicates["Synchro"] = assembleJSONPredicate(g_CurrentGesture.Synchro);  // Adding Synchro Predicates

            // -------------- Gesture Locs -------------- //
            assembleJSONPredicateList(g_CurrentGesture.Loc, LocPredicates); // Adding Loc Predicates
            GesturePredicates.Add("Locs", LocPredicates);

            // -------------- Gesture Extras -------------- //
            assembleJSONPredicateList(g_CurrentGesture.ExtraPredicates, ExtraPredicates); // Adding Extra Predicates
            GesturePredicates.Add("Extras", ExtraPredicates);

            GestureAnnotation["Predicates"] = GesturePredicates; // Adding Predicates

            return GestureAnnotation;
        }

        private async void saveCurrentGestureAnnotation(string filename, JObject objToBuild)
        {
            //save gesture id to annotated id file
            string filepath = @"JSONs\ForPaper\"+ filename + ".json";
            StorageFile file = await g_RootFolder.CreateFileAsync(filepath, CreationCollisionOption.OpenIfExists);
            string string_to_send = JsonConvert.SerializeObject(objToBuild, Formatting.Indented);
            await FileIO.WriteTextAsync(file, string_to_send);
        }

        private void assembleJSONVariables(List<Variable> varList, JObject objToAppend)
        {
            JObject Variables = new JObject();
            JArray IndividualArray = new JArray();
            JArray EventualityArray = new JArray();

            foreach (Variable var in varList)
            {
                JObject newVar = new JObject();
                newVar["Name"] = var.name;

                if (var.type == ConstantValues.g_EventualityString)
                {
                    newVar["InitialTime"] = var.initialTime;
                    newVar["FinalTime"] = var.finalTime;
                    EventualityArray.Add(newVar);
                }

                else
                    IndividualArray.Add(newVar);
            }

            Variables.Add("IndividualVariables", IndividualArray);
            Variables.Add("EventualityVariables", EventualityArray);
            objToAppend["Variables"] = Variables;
        }

        private void assembleJSONSpatiotemporal(List<Spatiotemporal> spatiotemporalList, JObject objToAppend)
        {
            JArray spatiotemporals = new JArray();

            foreach (Spatiotemporal var in spatiotemporalList)
            {
                JObject Spatiotemporal = new JObject();
                JArray Coordinates = new JArray();
                Spatiotemporal["Name"] = var.name;
                Coordinates.Add(var.posX);
                Coordinates.Add(var.posY);
                Coordinates.Add(var.posZ);
                Spatiotemporal.Add("Coordinates", Coordinates);
                Spatiotemporal["Time"] = var.time;
                spatiotemporals.Add(Spatiotemporal);
            }

            objToAppend.Add("Spatiotemporals", spatiotemporals);
        }

        private void assembleJSONMapping(List<Mapping> mappingList, JObject objToAppend)
        {
            JArray mappings = new JArray();

            foreach (Mapping var in mappingList)
            {
                JObject mapping = new JObject();
                mapping["Name"] = var.name;
                mapping["Spatiotemporal"] = var.physicalLocation.name;
                mapping["Transformation"] = new JObject();//var.transformation; NEED TO FIGURE OUT HOW TO REPRESENT THIS
                mappings.Add(mapping);
            }

            objToAppend.Add("Mappings", mappings);
        }
        
        private JObject assembleJSONPredicate(Predicate pPred)
        {
            JObject predicate = new JObject();
            JArray sortArray = new JArray();

            predicate["Name"] = pPred.name;
            predicate["Arity"] = pPred.arity;

            foreach (Type sort in pPred.sort)
            {
                sortArray.Add(sort.Name);
            }
            predicate.Add("Sort", sortArray);

            assembleJSONVariables(pPred.variables, predicate);

            assembleJSONMapping(pPred.virtualMappings, predicate);

            return predicate;
        }

        private void assembleJSONPredicateList(List<Predicate> predicateList, JArray arrayToAppend)
        {
            foreach (Predicate pred in predicateList)
            {
                JObject predicate = assembleJSONPredicate(pred);
                arrayToAppend.Add(predicate);
            }
        }

        private JObject assembleJSONShape(GestureShape pShape)
        {
            JObject GestureShape = new JObject();
            JObject ShapeVariables = new JObject();
            JObject ShapePredicates = new JObject();
            JArray ShapeIndividualVariables = new JArray();
            JArray ShapeEventualityVariables = new JArray();
            JArray ShapeComponents = new JArray();
            JArray ShapePoses = new JArray();
            JArray ShapeOrientations = new JArray();
            JArray ShapeSeparations = new JArray();

            assembleJSONVariables(pShape.Variables, GestureShape);

            assembleJSONPredicateList(pShape.Component, ShapeComponents);
            ShapePredicates.Add("Components", ShapeComponents);
            assembleJSONPredicateList(pShape.Pose, ShapePoses);
            ShapePredicates.Add("Poses", ShapePoses);
            assembleJSONPredicateList(pShape.Orientation, ShapeOrientations);
            ShapePredicates.Add("Orientations", ShapeOrientations);
            assembleJSONPredicateList(pShape.Separation, ShapeSeparations);
            ShapePredicates.Add("Separations", ShapeSeparations);
            ShapePredicates["Dependence"] = assembleJSONPredicate(pShape.Dependence);
            GestureShape["Predicates"] = ShapePredicates;

            return GestureShape;
        }

        private void assembleJSONShapeList(List<GestureShape> shapeList, JArray arrayToAppend)
        {
            foreach (GestureShape shape in shapeList)
            {
                JObject gShape = assembleJSONShape(shape);
                arrayToAppend.Add(gShape);
            }
        }

        private void assembleJSONMovement(GestureMovement pMovement, JObject objToAppend)
        {
            JArray MovementTrajectories = new JArray();
            JArray MovementDirections = new JArray();

            assembleJSONVariables(pMovement.Variables, objToAppend);
            assembleJSONSpatiotemporal(pMovement.Points, objToAppend);

            objToAppend["MainPlane"] = assembleJSONPredicate(pMovement.MainPlane);

            assembleJSONPredicateList(pMovement.Trajectories, MovementTrajectories);
            objToAppend.Add("Trajectories", MovementTrajectories);

            assembleJSONPredicateList(pMovement.Directions, MovementDirections);
            objToAppend.Add("Directions", MovementDirections);
        }

        private JObject constructionNetworkXJSON()
        {
            JObject GestureAnnotation = new JObject();

            GestureAnnotation["directed"] = true;
            GestureAnnotation["graph"] = new JObject();

            string varNodeID = createNode("Gesture" + g_CurrentGesture.name); // Main Gesture

            if(g_CurrentGesture.Variables.Count > 0)
            {
                assembleGraphVG(g_CurrentGesture.Variables, varNodeID);
            }
            if (g_CurrentGesture.Spatiotemporals.Count > 0)
            {
                assembleGraphSG(g_CurrentGesture.Spatiotemporals, varNodeID);
            }
            if (g_CurrentGesture.Mappings.Count > 0)
            {
                assembleGraphMG(g_CurrentGesture.Mappings, varNodeID);
            }
                
            assembleGraphCG(varNodeID);
            assembleGraphGPG(varNodeID);

            GestureAnnotation.Add("nodes", g_Nodes);
            GestureAnnotation.Add("links", g_Edges);
            GestureAnnotation["multigraph"] = false;

            return GestureAnnotation;
        }

        private void assembleGraphVG(List<Variable> varList, string parent)
        {
            string newVGnodeID = createNode("VG"); // Variable Group
            string newIVGnodeID = "";
            string newEVGnodeID = "";

            bool indExists = false;
            bool eventExists = false;

            foreach (Variable var in varList)
            {
                if (var.type == ConstantValues.g_IndividualString)
                {
                    indExists = true;
                }
                else
                {
                    eventExists = true;
                }
            }

            if(indExists)
            {
                newIVGnodeID = createNode("IVG"); // Individual Variable Group
                createEdge(newVGnodeID, newIVGnodeID);
            }
            if(eventExists)
            {
                newEVGnodeID = createNode("EVG"); // Eventuality Variable Group          
                createEdge(newVGnodeID, newEVGnodeID);
            } 

            foreach (Variable var in varList)
            {
                if(var.type == ConstantValues.g_IndividualString)
                {
                    string varNodeID = createNode("IV"); // Individual Variable
                    string nameNodeID = createNode("NAME"); // Individual Variable Name 
                    string actualValueNodeID = createNode(var.name); // Individual Variable Value

                    createEdge(newIVGnodeID, varNodeID);
                    createEdge(varNodeID, nameNodeID);
                    createEdge(nameNodeID, actualValueNodeID);
                }
                else
                {
                    string varNodeID = createNode("EV"); // Eventuality Variable
                    string nameNodeID = createNode("NAME"); // Eventuality Variable Name 
                    string actualValueNodeID = createNode(var.name); // Eventuality Variable Value
                    string initialTimeNodeID = createNode("TIME"); // Eventuality Variable Name 
                    string actualValueNodeID2 = createNode(var.initialTime.ToString()); // Eventuality Variable Value
                    string finalTimeNodeID = createNode("TIME"); // Eventuality Variable Name 
                    string actualValueNodeID3 = createNode(var.finalTime.ToString()); // Eventuality Variable Value

                    createEdge(newEVGnodeID, varNodeID);
                    createEdge(varNodeID, nameNodeID);
                    createEdge(varNodeID, initialTimeNodeID);
                    createEdge(varNodeID, finalTimeNodeID);
                    createEdge(nameNodeID, actualValueNodeID);
                    createEdge(initialTimeNodeID, actualValueNodeID2);
                    createEdge(finalTimeNodeID, actualValueNodeID3);
                }

            }

            createEdge(parent, newVGnodeID);
        }

        private void assembleGraphSG(List<Spatiotemporal> spatiotempList, string parent)
        {
            string newSGnodeID = createNode("SG"); // Spatiotemporal Group

            foreach (Spatiotemporal spatiotemp in spatiotempList)
            {
                string spatiotempNodeID = createNode("S"); // Spatiotemporal 
                string nameNodeID = createNode("NAME"); // Spatiotemporal Name 
                string actualValueNodeID = createNode(spatiotemp.name); // Spatiotemporal Name Value
                string coordinatesNodeID = createNode("COORDINATES"); // Spatiotemporal Coordinates 
                string coordinateXNodeID = createNode("CX"); // Spatiotemporal Coordinate X
                string coordinateYNodeID = createNode("CY"); // Spatiotemporal Coordinates Y
                string coordinateZNodeID = createNode("CZ"); // Spatiotemporal Coordinates Z
                string actualValueNodeID2 = createNode(spatiotemp.posX.ToString()); // Spatiotemporal Coordinate X Value
                string actualValueNodeID3 = createNode(spatiotemp.posY.ToString()); // Spatiotemporal Coordinate Y Value
                string actualValueNodeID4 = createNode(spatiotemp.posZ.ToString()); // Spatiotemporal Coordinate Z Value
                string timeNodeID = createNode("TIME"); // Spatiotemporal Time 
                string actualValueNodeID6 = createNode(spatiotemp.time.ToString()); // Spatiotemporal Time Value

                createEdge(newSGnodeID, spatiotempNodeID);
                createEdge(spatiotempNodeID, nameNodeID);
                createEdge(nameNodeID, actualValueNodeID);
                createEdge(spatiotempNodeID, coordinatesNodeID);
                createEdge(coordinatesNodeID, coordinateXNodeID);
                createEdge(coordinatesNodeID, coordinateYNodeID);
                createEdge(coordinatesNodeID, coordinateZNodeID);
                createEdge(coordinateXNodeID, actualValueNodeID2);
                createEdge(coordinateYNodeID, actualValueNodeID3);
                createEdge(coordinateZNodeID, actualValueNodeID4);
                createEdge(spatiotempNodeID, timeNodeID);
                createEdge(timeNodeID, actualValueNodeID6);
            }
            createEdge(parent, newSGnodeID);
        }

        private void assembleGraphMG(List<Mapping> mapList, string parent)
        {
            if (mapList.Count > 0)
            {
                string newMGnodeID = createNode("MG"); // Mapping Group

                foreach (Mapping map in mapList)
                {
                    string mapNodeID = createNode("M"); // Mapping
                    string nameNodeID = createNode("NAME"); // Mapping Name 
                    string actualValueNodeID = createNode(map.name); // Mapping Name Value
                    string spatiotempNodeID = createNode("S"); // Spatiotemporal 
                    string spatiotempnameNodeID = createNode("NAME"); // Spatiotemporal Name 
                    string actualValueNodeID2 = createNode(map.physicalLocation.name); // Spatiotemporal Name Value
                    string mapTransNodeID = createNode("TRANSFORMATION"); // Mapping Transformation 
                    string actualValueNodeID3 = createNode("ToComplete"); // Mapping Transformation Value

                    createEdge(newMGnodeID, mapNodeID);
                    createEdge(mapNodeID, nameNodeID);
                    createEdge(nameNodeID, actualValueNodeID);
                    createEdge(mapNodeID, spatiotempNodeID);
                    createEdge(spatiotempNodeID, spatiotempnameNodeID);
                    createEdge(spatiotempnameNodeID, actualValueNodeID2);
                    createEdge(mapNodeID, mapTransNodeID);
                    createEdge(mapTransNodeID, actualValueNodeID3);
                }
                createEdge(parent, newMGnodeID);
            }
        }

        private void assembleGraphCG(string parent)
        {
            if (g_CurrentGesture.ContextVars.Count > 0 || g_CurrentGesture.ContextPreds.Count > 0)
            {
                string newCGnodeID = createNode("CG"); // Context Group

                if (g_CurrentGesture.ContextVars.Count > 0)
                {
                    assembleGraphVG(g_CurrentGesture.ContextVars, newCGnodeID);
                }
                if (g_CurrentGesture.ContextPreds.Count > 0)
                {
                    assembleGraphPG(g_CurrentGesture.ContextPreds, newCGnodeID);
                }

                createEdge(parent, newCGnodeID);
            }
        }

        private void assembleGraphP(Predicate pred, string parent)
        {
            string predNodeID = createNode("P"); // Predicate
            string nameNodeID = createNode("NAME"); // Predicate Name 
            string actualValueNodeID = createNode(pred.name); // Predicate Name Value
            string arityNodeID = createNode("ARITY"); // Predicate Arity 
            string actualValueNodeID2 = createNode(pred.arity.ToString()); // Predicate Arity Value
            string sortNodeID = createNode("SORT"); // Predicate Sort 

            foreach (Type sortElem in pred.sort)
            {
                string typeString = sortElem.Name;

                string typeNodeID = createNode("TYPE"); // Sort Type
                string actualTypeNodeID = createNode(typeString); // Sort Type Value

                createEdge(sortNodeID, typeNodeID);
                createEdge(typeNodeID, actualTypeNodeID);
            }

            assembleGraphVG(pred.variables, predNodeID);
            assembleGraphMG(pred.virtualMappings, predNodeID);

            createEdge(parent, predNodeID);
            createEdge(predNodeID, nameNodeID);
            createEdge(nameNodeID, actualValueNodeID);
            createEdge(predNodeID, arityNodeID);
            createEdge(arityNodeID, actualValueNodeID2);
            createEdge(predNodeID, sortNodeID);
        }

        private void assembleGraphPG(List<Predicate> predList, string parent)
        {
            string newPGnodeID = createNode("PG"); // Predicate Group

            foreach(Predicate pred in predList)
            {
                assembleGraphP(pred, newPGnodeID);
            }
            createEdge(parent,newPGnodeID);
        }

        private void assembleGraphGPG(string parent)
        {
            string newGPGnodeID = createNode("LPG"); // Large Predicate Group
            string newTPGnodeID = createNode("TaG"); // Taxonomy Predicate Group
            
            assembleGraphPG(g_CurrentGesture.TaxClass, newTPGnodeID);

            Debug.WriteLine("Count");
            Debug.WriteLine(g_CurrentGesture.Shape.Count);
            if (g_CurrentGesture.Shape.Count > 0)
            {
                assembleGraphSPG(g_CurrentGesture.Shape, newGPGnodeID);
            }

            assembleGraphMov(g_CurrentGesture.Movement, newGPGnodeID);

            if (g_CurrentGesture.Exemplifies.Count > 0)
            {
                string newEPGnodeID = createNode("ExG"); // Exemplifies Predicate Group
                assembleGraphPG(g_CurrentGesture.Exemplifies, newEPGnodeID);
                createEdge(newGPGnodeID, newEPGnodeID);
            }

            if (!g_CurrentGesture.Synchro.Equals(default(Predicate)))
            {
                string newSyPGnodeID = createNode("SyG"); // Synchro Predicate Group
                assembleGraphP(g_CurrentGesture.Synchro, newSyPGnodeID);
                createEdge(newGPGnodeID, newSyPGnodeID);
            }

            if (g_CurrentGesture.Loc.Count > 0)
            {
                string newLPGnodeID = createNode("LoG"); // Loc Predicate Group
                assembleGraphPG(g_CurrentGesture.Loc, newLPGnodeID);
                createEdge(newGPGnodeID, newLPGnodeID);
            }

            assembleGraphPG(g_CurrentGesture.ExtraPredicates, newGPGnodeID);

            createEdge(newGPGnodeID, newTPGnodeID);

            createEdge(parent, newGPGnodeID);
        }

        private void assembleGraphSPG(List<GestureShape> shapeList, string parent)
        {
            string newSPGnodeID = createNode("ShG"); // Shape Predicate Group

            foreach(GestureShape shape in shapeList)
            {
                string shapeNodeID = createNode("Sh"); // Shape Group
                assembleGraphVG(shape.Variables, shapeNodeID); // Shape Variables
                assembleGraphShPG(shape, shapeNodeID); // Shape Predicates

                createEdge(newSPGnodeID, shapeNodeID);
            }
            createEdge(parent, newSPGnodeID);
        }

        private void assembleGraphShPG(GestureShape shape, string parent)
        {
            string newLPGnodeID = createNode("ShPG"); // Large Predicate Group
            string newComponentNodeID = createNode("COMPONENT"); // Shape Component Predicates
            string newPosesNodeID = createNode("POSE"); // Shape Poses Predicates     
            string newOrientationNodeID = createNode("ORIENTATION"); // Shape Orientations Predicates
            
            if(shape.Separation.Count > 0)
            {
                string newSeparationNodeID = createNode("SEPARATION"); // Shape Separations Predicates
                assembleGraphPG(shape.Separation, newSeparationNodeID);
                createEdge(newLPGnodeID, newSeparationNodeID);
            }
            if(!shape.Dependence.Equals(default(Predicate)))
            {
                string newDependenceNodeID = createNode("DEPENDENCE"); // Shape Dependence Predicate
                assembleGraphP(shape.Dependence, newDependenceNodeID);
                createEdge(newLPGnodeID, newDependenceNodeID);
            }

            assembleGraphPG(shape.Component, newComponentNodeID);
            assembleGraphPG(shape.Pose, newPosesNodeID);
            assembleGraphPG(shape.Orientation, newOrientationNodeID);

            createEdge(parent, newLPGnodeID);
            createEdge(newLPGnodeID, newComponentNodeID);
            createEdge(newLPGnodeID, newPosesNodeID);
            createEdge(newLPGnodeID, newOrientationNodeID);
        }

        private void assembleGraphMov(GestureMovement movement, string parent)
        {
            string newMPGnodeID = createNode("MvG"); // Movement Predicate Group
            string newMainPlanenodeID = createNode("MAINPLANE"); // Movement Predicate Group
            string newTrajnodeID = createNode("TRAJECTORY"); // Movement Predicate Group
            string newDirnodeID = createNode("DIRECTION"); // Movement Predicate Group

            assembleGraphVG(movement.Variables, newMPGnodeID); // Movement Variables
            assembleGraphSG(movement.Points, newMPGnodeID); // Movement Spatiotemporals
            assembleGraphP(movement.MainPlane, newMainPlanenodeID); // Movement Main Plane
            assembleGraphPG(movement.Trajectories, newTrajnodeID); // Movement Trajectories
            assembleGraphPG(movement.Directions, newDirnodeID); // Movement Directions

            createEdge(parent, newMPGnodeID);
            createEdge(newMPGnodeID, newMainPlanenodeID);
            createEdge(newMPGnodeID, newTrajnodeID);
            createEdge(newMPGnodeID, newDirnodeID);
        }

        private string createNode(string nodeName)
        {
            JObject newNode = new JObject();
            newNode["id"] = assignNodeID();
            newNode["name"] = nodeName;
            g_Nodes.Add(newNode);

            return (string)newNode["id"];
        }

        private void createEdge(string sourceNodeName, string targetNodeName)
        {
            JObject newEdge = new JObject();
            newEdge["source"] = sourceNodeName;
            newEdge["target"] = targetNodeName;
            g_Edges.Add(newEdge);
        }

        private string assignNodeID()
        {
            g_NodeID++;
            return ConstantValues.g_Node + g_NodeID.ToString();
        }
    }
}

using Newtonsoft.Json;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TheAnnotator9000
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class MainPage : Page
    {
        private Type g_VariableType;
        private Type g_PredicateType;
        private Type g_MappingType;
        private Type g_SpatiotemporalType;

        private JArray g_GestureDatabase;
        private StorageFolder g_RootFolder;
        private MediaPlayer g_MediaPlayer;
        private string g_SelectedGestureID;

        private int g_GestureCounter;
        private string g_CurrentArm;

        private bool g_RightExists;
        private bool g_LeftExists;
        private int g_NumPlaceholders;
        private int g_CurrentPlaceholder;

        private bool g_FirstTimeIndVar;

        public MainPage()
        {
            this.InitializeComponent();

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

            g_GestureCounter = 0;

            g_CurrentArm = "";

            g_RightExists = false;
            g_LeftExists = false;

            g_FirstTimeIndVar = true;

            //IndVarComboBox.Items.Add("Ejemplo 2");
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
            string filepath = @"JSONs\testJSON2.json";
            StorageFile file = await g_RootFolder.GetFileAsync(filepath);
            var data = await FileIO.ReadTextAsync(file);
            g_GestureDatabase = JArray.Parse(data);
        }

        private async void gestureVideoPlayback(JObject pSelectedGesture)
        {
            string filepath = @"Videos\";
            filepath = filepath + pSelectedGesture["Gesture Code"].ToString()[0];
            filepath = filepath + pSelectedGesture["Gesture Code"].ToString()[0];
            filepath += pSelectedGesture["Gesture Code"].ToString()[2] + ".avi";

            StorageFile file = await g_RootFolder.GetFileAsync(filepath);
            GestureRecording.SetMediaPlayer(g_MediaPlayer);
            g_MediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
            g_MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(Convert.ToInt32(pSelectedGesture["Gesture Spotted in Color at"]) - 2);
            g_MediaPlayer.Play();

            SpeechTranscriptionText.Text = pSelectedGesture["Speech Context"].ToString();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(10);
            timer.Start();
            timer.Tick += (o, args) =>
            {
                timer.Start();
                g_MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(Convert.ToInt32(pSelectedGesture["Gesture Spotted in Color at"]) - 2);
            };
        }

        private void startGestureAnnotation()
        {
            TaxonomyAnnotator();   
        }

        private void TaxonomyAnnotator()
        {
            TaxonomyAnnotatorGrid.Visibility = Visibility.Visible;
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton thisButton = (ToggleButton)sender;
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
                            /*List<Variable> gestVar = new List<Variable>();
                            gestVar.Add(g_IndividualVariables[g_IndividualVariables.Count - 1]);
                            g_Predicates.Add(createPredicate(thisButton.Content.ToString(), gestVar));*/
                        }
                        else
                        {
                            /*createIndividualVariable("Var" + g_GestureCounter.ToString());
                            g_GestureCounter++;*/
                        }
                    }
                }
            }

            TaxonomyAnnotatorGrid.Visibility = Visibility.Collapsed;
            RightArmText.Visibility = Visibility.Visible;
            YesButton.Visibility = Visibility.Visible;
            NoButton.Visibility = Visibility.Visible;
            g_CurrentArm = "Right";
        }

        

        private void ShapeCreationButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            if(selectedButton.Content.ToString() == "Yes")
            {
                if (g_CurrentArm == "Right")
                {
                    g_RightExists = true;
                }
                else
                {
                    g_LeftExists = true;
                }
                    
                //crear variables

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
                if(g_CurrentArm == "Right")
                {
                    RightArmText.Visibility = Visibility.Collapsed;
                    LeftArmText.Visibility = Visibility.Visible;
                    g_CurrentArm = "Left";
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
            string predicateName = "blabla" + selectedButton.Content.ToString();

            if (ArmPoseText.Visibility == Visibility.Visible)
            {
                //crear variables del brazo
                ArmPoseText.Visibility = Visibility.Collapsed;
                ArmOrientationText.Visibility = Visibility.Visible;
            }
            else if (HandPoseText.Visibility == Visibility.Visible)
            {
                //crear variables de la mano
                HandPoseText.Visibility = Visibility.Collapsed;
                HandOrientationText.Visibility = Visibility.Visible;
            }   
            
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
            string predicateName = "blabla" + selectedButton.Content.ToString();

            if (ArmOrientationText.Visibility == Visibility.Visible)
            {
                //crear variables del brazo
                ArmOrientationText.Visibility = Visibility.Collapsed;
                HandPoseText.Visibility = Visibility.Visible;
                ExtendedPoseButton.Visibility = Visibility.Visible;
                SemiExtendedPoseButton.Visibility = Visibility.Visible;
                NoExtendedPoseButton.Visibility = Visibility.Visible;
            }
            else if (HandOrientationText.Visibility == Visibility.Visible)
            {
                //crear variables de la mano
                HandOrientationText.Visibility = Visibility.Collapsed;
                LittleFingerText.Visibility = Visibility.Visible;
                FingerDescriptionText.Visibility = Visibility.Visible;
                FingerExtendedPoseButton.Visibility = Visibility.Visible;
                FingerSemiExtendedPoseButton.Visibility = Visibility.Visible;
                FingerNoExtendedPoseButton.Visibility = Visibility.Visible;
            }

            UpOrientationButton.Visibility = Visibility.Collapsed;
            DownOrientationButton.Visibility = Visibility.Collapsed;
            LeftOrientationButton.Visibility = Visibility.Collapsed;
            RightOrientationButton.Visibility = Visibility.Collapsed;
            ForwardOrientationButton.Visibility = Visibility.Collapsed;
        }

        private void FingerPoseButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            string predicateName = "blabla" + selectedButton.Content.ToString();

            if (LittleFingerText.Visibility == Visibility.Visible)
            {
                //create
                LittleFingerText.Visibility = Visibility.Collapsed;
                RingFingerText.Visibility = Visibility.Visible;
            }
            else if (RingFingerText.Visibility == Visibility.Visible)
            {
                //create
                RingFingerText.Visibility = Visibility.Collapsed;
                MiddleFingerText.Visibility = Visibility.Visible;
            }
            else if (MiddleFingerText.Visibility == Visibility.Visible)
            {
                //create
                MiddleFingerText.Visibility = Visibility.Collapsed;
                IndexFingerText.Visibility = Visibility.Visible;
            }
            else if (IndexFingerText.Visibility == Visibility.Visible)
            {
                //create
                IndexFingerText.Visibility = Visibility.Collapsed;
                ThumbFingerText.Visibility = Visibility.Visible;
            }
            else
            {
                //create
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

        }

        private void FingerOrientationButton_Click(object sender, RoutedEventArgs e)
        {
            if (LittleFingerText.Visibility == Visibility.Visible)
            {
                //create
                LittleFingerText.Visibility = Visibility.Collapsed;
                RingFingerText.Visibility = Visibility.Visible;
            }
            else if (RingFingerText.Visibility == Visibility.Visible)
            {
                //create
                RingFingerText.Visibility = Visibility.Collapsed;
                MiddleFingerText.Visibility = Visibility.Visible;
            }
            else if (MiddleFingerText.Visibility == Visibility.Visible)
            {
                //create
                MiddleFingerText.Visibility = Visibility.Collapsed;
                IndexFingerText.Visibility = Visibility.Visible;
            }
            else if (IndexFingerText.Visibility == Visibility.Visible)
            {
                //create
                IndexFingerText.Visibility = Visibility.Collapsed;
                ThumbFingerText.Visibility = Visibility.Visible;
            }
            else
            {
                //create
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
        }

        private void DoneWithFingersButton_Click(object sender, RoutedEventArgs e)
        {
            //Create
            if (g_CurrentArm == "Right")
            {
                LeftArmText.Visibility = Visibility.Visible;
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                g_CurrentArm = "Left";
            }
            else
            {
                LeftArmText.Visibility = Visibility.Collapsed;
                YesButton.Visibility = Visibility.Collapsed;
                NoButton.Visibility = Visibility.Collapsed;
                isDependentText.Visibility = Visibility.Visible;
                YesDependentButton.Visibility = Visibility.Visible;
                NoDependentButton.Visibility = Visibility.Visible;
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

        private void OptionDependentButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            string predicateName = "blabla" + selectedButton.Content.ToString();

            if (selectedButton.Content.ToString() == "Yes")
            {
                // predicate yes
            }
            else
            {
                // predicate no
            }

            isDependentText.Visibility = Visibility.Collapsed;
            YesDependentButton.Visibility = Visibility.Collapsed;
            NoDependentButton.Visibility = Visibility.Collapsed;

            AnotherShapeButton.Visibility = Visibility.Visible;
            ShapeDoneButton.Visibility = Visibility.Visible;
        }

        private void HandFingerButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            string predicateName = "blabla" + selectedButton.Content.ToString();

            if(selectedButton.Content.ToString() == "Yes")
            {
                // predicate yes
            }
            else
            {
                // predicate no
            }
        }

        private void AnotherShapeButton_Click(object sender, RoutedEventArgs e)
        {
            RightArmText.Visibility = Visibility.Visible;
            YesButton.Visibility = Visibility.Visible;
            NoButton.Visibility = Visibility.Visible;
            g_CurrentArm = "Right";

            AnotherShapeButton.Visibility = Visibility.Collapsed;
            ShapeDoneButton.Visibility = Visibility.Collapsed;
        }

        private void ShapeDoneButton_Click(object sender, RoutedEventArgs e)
        {
            AnotherShapeButton.Visibility = Visibility.Collapsed;
            ShapeDoneButton.Visibility = Visibility.Collapsed;

            if (g_RightExists)
            {
                RightPlaceholdersText.Visibility = Visibility.Visible;
                NumPlaceholdersTextbox.Visibility = Visibility.Visible;
                DoneNumPlaceholdersButton.Visibility = Visibility.Visible;
                g_CurrentArm = "Right";
            }
            else if (g_LeftExists)
            {
                LeftPlaceholdersText.Visibility = Visibility.Visible;
                NumPlaceholdersTextbox.Visibility = Visibility.Visible;
                DoneNumPlaceholdersButton.Visibility = Visibility.Visible;
                g_CurrentArm = "Left";
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

                for (int counter = 0; counter < g_NumPlaceholders; counter++)
                {
                    //create
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
            string predicateName = "blabla" + selectedButton.Content.ToString();

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

                if (g_CurrentArm == "Right" && g_LeftExists)
                {
                    NumPlaceholdersTextbox.Text = "";
                    LeftPlaceholdersText.Visibility = Visibility.Visible;
                    NumPlaceholdersTextbox.Visibility = Visibility.Visible;
                    DoneNumPlaceholdersButton.Visibility = Visibility.Visible;
                    g_CurrentArm = "Left";
                }
                else
                {
                    DoneTrajectoryButton.Visibility = Visibility.Visible;
                }
            }           
        }

        private void DoneTrajectoringButton_Click(object sender, RoutedEventArgs e)
        {
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
                    g_CurrentArm = "Right";
                }
                else
                {
                    ExemplifiesGrid.Visibility = Visibility.Visible;
                    LeftExemplefiesText.Visibility = Visibility.Visible;
                    g_CurrentArm = "Left";
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
                //create

                IndVarComboBox.Items.Add(IndividualTextbox.Text);
                IndVarComboBox2.Items.Add(IndividualTextbox.Text);
                IndividualTextbox.Text = "";
            }   
        }

        private void DoneAssigningVarButton_Click(object sender, RoutedEventArgs e)
        {
            if (IndVarComboBox.SelectedItem != null)
            {
                // create

                if (g_CurrentArm == "Right")
                {
                    RightExemplefiesText.Visibility = Visibility.Collapsed;

                    if (g_LeftExists)
                    {
                        LeftExemplefiesText.Visibility = Visibility.Visible;
                        g_CurrentArm = "Left";
                    }
                    else
                    {
                        ExemplifiesGrid.Visibility = Visibility.Collapsed;
                        g_CurrentArm = "Right";

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
                //create

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
                    g_CurrentArm = "Right";
                }
                else
                {
                    isLeftLocText.Visibility = Visibility.Visible;
                    g_CurrentArm = "Left";
                }
            }
            else
            {
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

                if(g_CurrentArm == "Right")
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

                if (g_CurrentArm == "Right")
                {
                    LocforLeftText.Visibility = Visibility.Visible;
                    YesLocButton.Visibility = Visibility.Visible;
                    NoLocButton.Visibility = Visibility.Visible;

                    g_CurrentArm = "Left";
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
                PhysicalLocComboBox.Items.Add(PhysicalLocTextbox.Text);

                PhysicalLocTextbox.Text = "";
            }
        }

        private void AssignLocButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (EventVarComboBox.SelectedItem != null && IndVarComboBox2.SelectedItem != null && PhysicalLocComboBox.SelectedItem != null)
            {
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

                if (g_CurrentArm == "Right")
                {
                    //create
                    g_CurrentArm = "Left";
                    YesLocButton.Visibility = Visibility.Visible;
                    NoLocButton.Visibility = Visibility.Visible;
                    isLeftLocText.Visibility = Visibility.Visible;
                }
                else
                {
                    //create
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

            saveGestureIDtoFile();
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            CoreApplication.Exit();
        }

        private async void saveGestureIDtoFile()
        {
            //save gesture id to annotated id file
            string filepath = @"JSONs\gesturesIDs.json";
            StorageFile file = await g_RootFolder.GetFileAsync(filepath);
            var data = await FileIO.ReadTextAsync(file);
            JArray g_GesturesID = JArray.Parse(data);
            g_GesturesID.Add(g_SelectedGestureID);
            string string_to_send = JsonConvert.SerializeObject(g_GesturesID, Formatting.None);
            await FileIO.WriteTextAsync(file, string_to_send);
        }
    }
}

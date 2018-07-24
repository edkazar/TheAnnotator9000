using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

    public struct Predicate
    {
        public string name;
        public List<string> sort;
        public int arity;
        public List<Variable> variables;
    }

    public struct Variable
    {
        public string name;
        public string type;
    }

    public struct GestureShape
    {
        public List<Variable> Variables; // Variables created or used by the gesture
        public List<Predicate> Component; // For dependences bewteen body parts
        public List<Predicate> Pose; // Whether the body part is extended or not
        public List<Predicate> Orientation; // Position towards where the body part is facing
        public List<Predicate> Separated; // Whether the body part is separated (usually for fingers)
        public List<Predicate> ExtraPredicates; // For extra predicates
    };

    public struct GestureMovement
    {
        public List<Variable> Variables; // Variables created or used by the gesture
        public Predicate InitialPoint; // Initial point of the trajectory
        public Predicate TransitionPoint; // Transition point of the trajectory
        public Predicate FinalPoint; // Finial point of the trajectory
        public List<Predicate> Plane; // Planes of motion
        public List<Predicate> Trajectory; // Name of the icon annotation
    };

    public struct GestureAnnotation //ID of gesture will be the name of the JSON file
    {
        public List<Variable> Variables; // Variables created or used by the gesture
        public List<Predicate> TaxClass; // Taxonomy class of the gesture
        public List<GestureShape> Shape; // Struct describing the gesture's shape
        public Predicate Dependence; // Whether or not there is a dependence between hands
        public List<GestureMovement> Movement; // Struct describing the gesture's movement
        public Predicate Synchro; // Whether or not the gesture is synchronized with an event
        public Predicate Loc; // Spatiotemporal information of the gesture
        public List<Predicate> Exemplifies; // Semantic concepts described by the gesture
        public List<Predicate> ExtraPredicates; // For extra predicates
    };

    public sealed partial class MainPage : Page
    {
        private JArray g_GestureDatabase;
        private StorageFolder g_RootFolder;
        private MediaPlayer g_MediaPlayer;

        private List<Variable> g_IndividualVariables;
        private List<Variable> g_EventualityVariables;
        private List<Predicate> g_Predicates;
       
        private int g_GestureCounter;
        private string currentArm;

        private bool g_RightExists;
        private bool g_LeftExists;
        private int g_NumPlaceholders;
        private int g_CurrentPlaceholder;

        public MainPage()
        {
            this.InitializeComponent();
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
            g_MediaPlayer = new MediaPlayer();
            g_MediaPlayer.IsLoopingEnabled = true;

            g_GestureCounter = 0;

            g_IndividualVariables = new List<Variable>();
            g_EventualityVariables = new List<Variable>();
            g_Predicates = new List<Predicate>();

            currentArm = "";

            g_RightExists = false;
            g_LeftExists = false;
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
                    GestureIDText.Text += selectedGesture["Gesture Code"].ToString();
                    GestureIDText.Visibility = Visibility.Visible;
                    GestureRecording.Visibility = Visibility.Visible;
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
                            List<Variable> gestVar = new List<Variable>();
                            gestVar.Add(g_IndividualVariables[g_IndividualVariables.Count - 1]);
                            g_Predicates.Add(createPredicate(thisButton.Content.ToString(), gestVar));
                        }
                        else
                        {
                            createIndividualVariable("Var" + g_GestureCounter.ToString());
                            g_GestureCounter++;
                        }
                    }
                }
            }

            TaxonomyAnnotatorGrid.Visibility = Visibility.Collapsed;
            RightArmText.Visibility = Visibility.Visible;
            YesButton.Visibility = Visibility.Visible;
            NoButton.Visibility = Visibility.Visible;
            currentArm = "Right";
        }

        private Variable createIndividualVariable(string pName)
        {
            Variable variable;
            variable.name = pName;
            variable.type = "Individual";
            g_IndividualVariables.Add(variable);
            IndVarComboBox.Items.Add(pName);
            return variable;
        }

        private Predicate createPredicate(string pName, List<Variable> pVars)
        {
            Predicate predicate;
            predicate.name = pName;
            predicate.arity = pVars.Count;
            predicate.variables = pVars;

            List<string> sort = new List<string>();

            foreach(Variable var in pVars)
            {
                sort.Add(var.type);
            }
            predicate.sort = sort;

            return predicate;
        }

        private void ShapeCreationButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            if(selectedButton.Content.ToString() == "Yes")
            {
                if (currentArm == "Right")
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
                if(currentArm == "Right")
                {
                    RightArmText.Visibility = Visibility.Collapsed;
                    LeftArmText.Visibility = Visibility.Visible;
                    currentArm = "Left";
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
            if (currentArm == "Right")
            {
                LeftArmText.Visibility = Visibility.Visible;
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                currentArm = "Left";
            }
            else
            {
                LeftArmText.Visibility = Visibility.Collapsed;
                YesButton.Visibility = Visibility.Collapsed;
                NoButton.Visibility = Visibility.Collapsed;
                AnotherShapeButton.Visibility = Visibility.Visible;
                ShapeDoneButton.Visibility = Visibility.Visible;
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
            currentArm = "Right";

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
                currentArm = "Right";
            }
            else if (g_LeftExists)
            {
                LeftPlaceholdersText.Visibility = Visibility.Visible;
                NumPlaceholdersTextbox.Visibility = Visibility.Visible;
                DoneNumPlaceholdersButton.Visibility = Visibility.Visible;
                currentArm = "Left";
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

            if (g_CurrentPlaceholder<=g_NumPlaceholders)
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

                if (currentArm == "Right" && g_LeftExists)
                {
                    LeftPlaceholdersText.Visibility = Visibility.Visible;
                    NumPlaceholdersTextbox.Visibility = Visibility.Visible;
                    DoneNumPlaceholdersButton.Visibility = Visibility.Visible;
                    currentArm = "Left";
                }
                else
                {
                    DoneTrajectoryButton.Visibility = Visibility.Visible;
                }
            }

            
        }

        private void DoneTrajectoringButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("CACHIN");

            DoneTrajectoryButton.Visibility = Visibility.Collapsed;
        }
    }
}

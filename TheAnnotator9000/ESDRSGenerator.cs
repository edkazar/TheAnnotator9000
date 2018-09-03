using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Media3D;

namespace TheAnnotator9000
{
    public struct Predicate
    {
        public string name; // Predicate given name
        public List<Type> sort; // Expected arguments' type
        public int arity; // Number of elements of the predicate
        public List<Variable> variables; // Variables referenced by the predicate
        public List<Mapping> virtualMappings; // Virtual mapping the predicate can refer to
    }

    public struct Variable
    {
        public string name; // Name of the variable
        public string type; // Individual or Eventuality
        public TimeSpan initialTime; // For eventuality variables, point in time when event starts
        public TimeSpan finalTime; // For eventuality variables, point in time when event finishes
    }

    public struct Mapping
    {
        public string name; // For ease purposes, not part of structure
        public Matrix3D transformation; // Geometric transformation matrix from physical to virtual space
        public Spatiotemporal physicalLocation; // Physical location in time
    }

    public struct Spatiotemporal
    {
        public string name; // For ease purposes, not part of structure
        public int posX; // Physical X-axis coordinate
        public int posY; // Physical Y-axis coordinate
        public int posZ; // Physical Z-axis coordinate
        public TimeSpan time; // Moment in time when the coordinate resides
    }

    public struct GestureShape
    {
        public List<Variable> Variables; // Variables created or used by the gesture
        public List<Predicate> Component; // For dependences between body parts
        public List<Predicate> Pose; // Whether the body part is extended or not
        public List<Predicate> Orientation; // Position towards where the body part is facing
        public List<Predicate> Separation; // Whether the body part is separated (usually for fingers)
        public Predicate Dependence; // Whether or not there is a dependence between hands
    };

    public struct GestureMovement
    {
        public List<Variable> Variables; // Variables created or used by the gesture
        public List<Spatiotemporal> Points; // Motion placeholders (zero-velocity points)
        public Predicate MainPlane; // Main plane of motion
        public List<Predicate> Trajectories; // Name of the icon annotation
        public List<Predicate> Directions; // Name of the icon annotation
    };

    public struct GestureAnnotation //ID of gesture will be the name of the JSON file
    {
        public string name; // For ease purposes, not part of structure
        public List<Variable> Variables; // Variables created or used by the gesture
        public List<Variable> ContextVars; // For variables of previous utterances
        public List<Predicate> ContextPreds; // For variables of previous utterances
        public List<Predicate> TaxClass; // Taxonomy class of the gesture
        public List<GestureShape> Shape; // Struct describing the gesture's shape 
        public GestureMovement Movement; // Struct describing the gesture's movement
        public List<Predicate> Exemplifies; // Semantic concepts described by the gesture
        public Predicate Synchro; // Whether or not the gesture is synchronized with an event
        public List<Predicate> Loc; // Spatiotemporal information of the gesture
        public List<Predicate> ExtraPredicates; // For extra predicates
        public List<Spatiotemporal> Spatiotemporals;
        public List<Mapping> Mappings;
    };

    class ESDRSGenerator
    {
        private Dictionary<string, Variable> g_IndividualVariables;
        private Dictionary<string, Variable> g_EventualityVariables;
        private Dictionary<string, Mapping> g_Mappings;
        private Dictionary<string, Spatiotemporal> g_Spatiotemporals;
        private List<Predicate> g_Predicates;

        private List<GestureAnnotation> g_GestureAnnotations;

        public ESDRSGenerator()
        {
            g_IndividualVariables = new Dictionary<string, Variable>();
            g_EventualityVariables = new Dictionary<string, Variable>();
            g_Mappings = new Dictionary<string, Mapping>();
            g_Spatiotemporals = new Dictionary<string, Spatiotemporal>();
            g_Predicates = new List<Predicate>();
            g_GestureAnnotations = new List<GestureAnnotation>();
        }

        public GestureAnnotation createGestureAnnotation(string pGestureID)
        {
            GestureAnnotation newGesture = new GestureAnnotation();
            newGesture.Variables = new List<Variable>();
            newGesture.TaxClass = new List<Predicate>();
            newGesture.ContextVars = new List<Variable>();
            newGesture.ContextPreds = new List<Predicate>();
            newGesture.Shape = new List<GestureShape>();
            newGesture.Exemplifies = new List<Predicate>();
            newGesture.Loc = new List<Predicate>();
            newGesture.ExtraPredicates = new List<Predicate>();
            newGesture.Spatiotemporals = new List<Spatiotemporal>();
            newGesture.Mappings = new List<Mapping>();
            newGesture.name = pGestureID;
            return newGesture;
        }

        public GestureShape createGestureShape()
        {
            GestureShape newShape = new GestureShape();
            newShape.Variables = new List<Variable>();
            newShape.Component = new List<Predicate>();
            newShape.Pose = new List<Predicate>();
            newShape.Orientation = new List<Predicate>();
            newShape.Separation = new List<Predicate>();
            return newShape;
        }

        public GestureMovement createGestureMovement()
        {
            GestureMovement newMovement = new GestureMovement();
            newMovement.Variables = new List<Variable>();
            newMovement.Points = new List<Spatiotemporal>();
            newMovement.Trajectories = new List<Predicate>();
            newMovement.Directions = new List<Predicate>();
            return newMovement;
        }

        public Variable createVariable(string pName, string pType, TimeSpan pInitialTime, TimeSpan pFinalTime)
        {
            Variable variable;
            Variable toSearch;
            
            variable.name = pName;
            variable.type = pType;
            variable.initialTime = pInitialTime;
            variable.finalTime = pFinalTime;

            if (pType == "Individual")
            {   
                if(!g_IndividualVariables.TryGetValue(pName, out toSearch))
                     g_IndividualVariables.Add(pName, variable);
            }
            else
            {
                if (!g_EventualityVariables.TryGetValue(pName, out toSearch))
                    g_EventualityVariables.Add(pName, variable);
            }

            return variable;
        }

        public Predicate createPredicate(string pName, List<Type> pSort, List<string> pArguments)
        {
            Predicate predicate;
            List<Variable> tempVariables = new List<Variable>();
            List<Mapping> tempVirtualMappings = new List<Mapping>();

            predicate.name = pName;
            predicate.sort = pSort;
            predicate.arity = pSort.Count;

            int argumentCounter = 0;
            foreach(Type myType in pSort)
            {
                if (myType == typeof(Variable))
                {
                    Variable tempVariable;
                    bool indExists = g_IndividualVariables.TryGetValue(pArguments[argumentCounter], out tempVariable);
                    if (!indExists)
                        g_EventualityVariables.TryGetValue(pArguments[argumentCounter], out tempVariable);

                    tempVariables.Add(tempVariable);
                }
                else if (myType == typeof(Mapping))
                {
                    Mapping tempMapping;
                    g_Mappings.TryGetValue(pArguments[argumentCounter], out tempMapping);
                    tempVirtualMappings.Add(tempMapping);
                }
                argumentCounter++;
            }

            predicate.variables = tempVariables;
            predicate.virtualMappings = tempVirtualMappings;
            g_Predicates.Add(predicate);

            return predicate;
        }

        public Spatiotemporal createSpatiotemporal(string pName, int pPosX, int pPosY, int pPosZ, TimeSpan pTime)
        {
            Spatiotemporal tempSpatiotemporal;

            tempSpatiotemporal.name = pName;
            tempSpatiotemporal.posX = pPosX;
            tempSpatiotemporal.posY = pPosY;
            tempSpatiotemporal.posZ = pPosZ;
            tempSpatiotemporal.time = pTime;

            g_Spatiotemporals.Add(pName, tempSpatiotemporal);

            return tempSpatiotemporal;
        }

        public Mapping createMapping(string pName, Matrix3D pMatrix, string pSpatiotemporal)
        {
            Mapping tempMapping;

            tempMapping.name = pName;
            tempMapping.transformation = pMatrix;

            Spatiotemporal tempSpatiotemporal;
            g_Spatiotemporals.TryGetValue(pSpatiotemporal, out tempSpatiotemporal);
            tempMapping.physicalLocation = tempSpatiotemporal;

            g_Mappings.Add(pName, tempMapping);

            return tempMapping;
        }
    }
}

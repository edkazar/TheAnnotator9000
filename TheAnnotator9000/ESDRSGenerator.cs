using System;
using System.Collections.Generic;
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
        public int index; // Index in the list it belongs. For ease purposes, not part of structure
    }

    public struct Variable
    {
        public string name; // Name of the variable
        public string type; // Individual or Eventuality
        public TimeSpan initialTime; // For eventuality variables, point in time when event starts
        public TimeSpan finalTime; // For eventuality variables, point in time when event finishes
        public int index; // Index in the list it belongs. For ease purposes, not part of structure
    }

    public struct Mapping
    {
        public Matrix3D transformation; // Geometric transformation matrix from physical to virtual space
        public Spatiotemporal physicalLocation; // Physical location in time
        public int index; // Index in the list it belongs. For ease purposes, not part of structure
    }

    public struct Spatiotemporal
    {
        public int posX; // Physical X-axis coordinate
        public int posY; // Physical Y-axis coordinate
        public int posZ; // Physical Z-axis coordinate
        public TimeSpan time; // Moment in time when the coordinate resides
        public int index; // Index in the list it belongs. For ease purposes, not part of structure
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
        public int index; // Index in the list it belongs. For ease purposes, not part of structure
    };

    class ESDRSGenerator
    {
        private Dictionary<string,Variable> g_IndividualVariables;
        private Dictionary<string, Variable> g_EventualityVariables;
        private Dictionary<string, Mapping> g_Mappings;
        private Dictionary<string, Spatiotemporal> g_Spatiotemporals;
        private Dictionary<string, Predicate> g_Predicates;

        private List<GestureAnnotation> g_GestureAnnotations;

        public ESDRSGenerator()
        {
            g_IndividualVariables = new Dictionary<string, Variable>();
            g_EventualityVariables = new Dictionary<string, Variable>();
            g_Mappings = new Dictionary<string, Mapping>();
            g_Spatiotemporals = new Dictionary<string, Spatiotemporal>();
            g_Predicates = new Dictionary<string, Predicate>();

            g_GestureAnnotations = new List<GestureAnnotation>();

        }

        public string createVariable(string pName, string pType, TimeSpan pInitialTime, TimeSpan pFinalTime)
        {
            Variable variable;
            variable.name = pName;
            variable.type = pType;
            variable.initialTime = pInitialTime;
            variable.finalTime = pFinalTime;

            if (pType == "Individual")
            {
                variable.index = g_IndividualVariables.Count;
                g_IndividualVariables.Add(pName, variable);
            }
            else
            {
                variable.index = g_EventualityVariables.Count;
                g_EventualityVariables.Add(pName, variable);
            }

            //IndVarComboBox.Items.Add(pName);
            return variable.name;
        }

        public string createPredicate(string pName, List<Type> pSort, List<string> pArguments)
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
                    g_IndividualVariables.TryGetValue(pArguments[argumentCounter], out tempVariable);
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
            predicate.index = g_Predicates.Count;
            g_Predicates.Add(pName, predicate);

            return predicate.name;
        }
    }
}

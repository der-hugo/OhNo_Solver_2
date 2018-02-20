using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class FieldManager : MonoBehaviour
{

    #region PUBLIC MEMBERS

    public static FieldManager Singleton;

    public int Size { private set; get; }

    #region Types

    public enum FieldColor
    {
        Empty,
        Blue,
        Red
    }

    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    public class CalculationField
    {

        #region PUBLIC MEMBERS

        public bool Finished;

        public int X;
        public int Y;

        public FieldColor Color;
        public int TargetValue;

        public int Missing;

        public Dictionary<Direction, bool> PossibleDirections = new Dictionary<Direction, bool>(4)
        {
            { Direction.Up, true},
            { Direction.Down, true},
            { Direction.Left, true},
            { Direction.Right, true}
        };

        public Dictionary<Direction, CalculationField[]> Neighbours = new Dictionary<Direction, CalculationField[]>(4)
        {
            { Direction.Up, null},
            { Direction.Down, null},
            { Direction.Left, null},
            { Direction.Right, null}
        };

        #endregion PUBLIC MEMBERS

        #region CONSTRUCTOR

        public CalculationField(int initialTargetValue, FieldColor initialColor)
        {
            TargetValue = initialTargetValue;
            Color = initialColor;
        }

        #endregion CONSTRUCTOR


        #region PRIVATE METHODS

        private int GetPossibleDirectionsCount()
        {
            int count = 0;

            foreach (Direction direction in Directions)
            {
                if (PossibleDirections[direction])
                {
                    count++;
                }
            }

            return count;
        }

        private void UpdateNeighbours()
        {
            foreach (Direction direction in Directions)
            {
                Neighbours[direction] = NeighboursInDirection(direction);
            }
        }

        private CalculationField[] NeighboursInDirection(Direction direction)
        {
            var neighbours = new List<CalculationField>();


            switch (direction)
            {
                case Direction.Up:
                    for (var i = Y - 1; i >= 0; i--)
                    {
                        var nextNeighbour = Singleton.CalculationFields[X, i];

                        if (nextNeighbour.Color == FieldColor.Red) break;

                        neighbours.Add(nextNeighbour);
                    }
                    break;

                case Direction.Down:
                    for (var i = Y + 1; i < Singleton.Size; i++)
                    {
                        var nextNeighbour = Singleton.CalculationFields[X, i];
                        if (nextNeighbour.Color == FieldColor.Red) break;

                        neighbours.Add(nextNeighbour);
                    }
                    break;

                case Direction.Left:
                    for (var i = X - 1; i >= 0; i--)
                    {
                        var nextNeighbour = Singleton.CalculationFields[i, Y];
                        if (nextNeighbour.Color == FieldColor.Red) break;

                        neighbours.Add(nextNeighbour);
                    }
                    break;

                case Direction.Right:
                    for (var i = X + 1; i < Singleton.Size; i++)
                    {
                        var nextNeighbour = Singleton.CalculationFields[i, Y];
                        if (nextNeighbour.Color == FieldColor.Red) break;

                        neighbours.Add(nextNeighbour);
                    }
                    break;
            }

            return neighbours.ToArray();
        }


        private void UpdatePossibleDirections()
        {

            foreach (var direction in Directions)
            {
                // Skip already blocked directions
                if (!PossibleDirections[direction]) continue;

                var neighbours = Neighbours[direction];

                if (neighbours.Length == 0)
                {
                    // blocked in this direction
                    PossibleDirections[direction] = false;
                    Singleton.ChangeMade = true;
                    continue;
                }

                bool sawEmpty = false;
                foreach (CalculationField neighbour in neighbours)
                {
                    if (neighbour.Color == FieldColor.Blue) continue;

                    if (neighbour.Color == FieldColor.Empty)
                    {
                        sawEmpty = true;
                        break;
                    }
                }

                if (!sawEmpty)
                {
                    // blocked in this direction because already all filled blue
                    PossibleDirections[direction] = false;
                    Singleton.ChangeMade = true;
                }
            }
        }


        private void UpdateMissing()
        {
            int actualCount = 0;

            foreach (Direction direction in Directions)
            {
                var neighbours = Neighbours[direction];
                foreach (CalculationField neighbourField in neighbours)
                {
                    if (neighbourField.Color != FieldColor.Blue) break;

                    actualCount++;
                }
            }

            Missing = TargetValue - actualCount;
        }


        private void OverloadsField()
        {
            foreach (Direction direction in Directions)
            {
                if (!PossibleDirections[direction]) continue;

                // get first empty neighbour in direction
                var neighbours = Neighbours[direction];
                CalculationField emptyNeighbour = null;

                foreach (CalculationField neighbour in neighbours)
                {
                    if (neighbour.Color == FieldColor.Empty)
                    {
                        emptyNeighbour = neighbour;
                        break;
                    }
                }

                if (emptyNeighbour == null) continue;

                // set neighbour blue
                emptyNeighbour.Color = FieldColor.Blue;

                // update missing
                UpdateMissing();

                // if missing < 0 -> set neighbour red; continue;
                if (Missing < 0)
                {
                    Debug.Log(emptyNeighbour.X + "," + emptyNeighbour.Y + " would overload field " + X + "," + Y);
                    Debug.Log("---Setting field " + emptyNeighbour.X + "," + emptyNeighbour.Y + " to red.");
                    emptyNeighbour.Color = FieldColor.Red;
                    Singleton.ChangeMade = true;
                    return;
                }

                // set nieghbour empty
                emptyNeighbour.Color = FieldColor.Empty;
            }
        }

        private void FillAllNeighbours()
        {
            foreach (Direction direction in Directions)
            {
                var neighbours = Neighbours[direction];
                foreach (CalculationField neighbour in neighbours)
                {
                    if (neighbour.TargetValue != -1) continue;

                    if (neighbour.Color != FieldColor.Empty) continue;

                    neighbour.Color = FieldColor.Blue;
                    neighbour.Finished = true;
                    Singleton.ChangeMade = true;
                }
            }

            Finished = true;
            Singleton.ChangeMade = true;
        }


        private int GetNeighbourAmount()
        {
            int ret = 0;

            foreach (Direction direction in Directions)
            {
                var neighbours = Neighbours[direction];

                ret += neighbours.Length;
            }

            return ret;
        }

        private Direction GetOnlyDirection()
        {
            Direction ret = Direction.Up;

            foreach (Direction direction in Directions)
            {
                if (PossibleDirections[direction])
                {
                    return direction;
                }
            }

            Debug.LogError("No Direction is possible for field " + X + "," + Y);
            return ret;
        }

        private bool Satisfied()
        {
            if (Missing == 0)
            {
                return true;
            }

            return false;
        }

        private void BlockEndDirection(Direction direction)
        {

            // Skip direction if it is already blocked
            if (!PossibleDirections[direction]) return;

            var neighbours = Neighbours[direction];

            // If no neighbours in this direction
            // than field is blocked
            if (neighbours.Length == 0)
            {
                Debug.Log("---No neighbours in direction " + direction + ". => field is already blocked");
                PossibleDirections[direction] = false;
                Singleton.ChangeMade = true;
                return;
            }

            foreach (CalculationField neighbour in neighbours)
            {
                // Initially Set Fields can't be changed
                if (neighbour.TargetValue != -1) continue;

                switch (neighbour.Color)
                {
                    case FieldColor.Blue:
                        // Do nothing and go to next Field
                        break;

                    case FieldColor.Empty:
                        neighbour.Color = FieldColor.Red;
                        neighbour.Finished = true;
                        PossibleDirections[direction] = false;
                        Debug.Log("---Blocking in direction " + direction);
                        Singleton.ChangeMade = true;
                        return;
                }
            }

            // reaching this means the directions is blocked eihter by the field limit or
            // an already set red dot
            Debug.Log("---" + direction + " is blocked by field limit.");
            PossibleDirections[direction] = false;
            Singleton.ChangeMade = true;
        }

        private void BlockEnds()
        {
            foreach (Direction direction in Directions)
            {
                BlockEndDirection(direction);
            }
        }

        private void AddInDirection(Direction direction, int amount, bool goOn)
        {
            int targetAmount = amount;

            var neighbours = Neighbours[direction];
            foreach (CalculationField neighbour in neighbours)
            {
                switch (neighbour.Color)
                {
                    case FieldColor.Empty:
                        // Set field to blue, reduce amount by one and go on to next field
                        neighbour.Color = FieldColor.Blue;
                        targetAmount--;
                        Debug.Log("---Filling empty spot with blue at " + neighbour.X + "," + neighbour.Y);
                        neighbour.Finished = true;
                        Singleton.ChangeMade = true;
                        break;

                    case FieldColor.Blue:
                        // Do nothing and go to next field
                        if (goOn)
                        {
                            targetAmount--;
                        }
                        break;
                }

                if (targetAmount <= 0) return;
            }
        }

        #endregion PRIVATE METHODS


        #region PUBLIC METHODS


        public bool Step1_Satisfied()
        {
            // Skip already finished fields
            if (Finished) return false;

            if (Satisfied())
            {
                Debug.Log("Field " + X + "," + Y + " is satisfied.");

                BlockEnds();

                Finished = true;
                Singleton.ChangeMade = true;
                return true;
            }

            return false;
        }

        public bool Step2_SingleDirection()
        {
            // Skip already finished fields
            if (Finished) return false;

            if (Missing == 0) return false;

            if (GetPossibleDirectionsCount() == 1)
            {
                Debug.Log("Only one direction for field " + X + "," + Y);
                Direction dir = GetOnlyDirection();
                Debug.Log(dir);

                AddInDirection(dir, 1, false);
                return true;
            }

            return false;
        }

        public bool Step3_MatchNeigbourAmount()
        {
            // Skip already finished fields
            if (Finished) return false;

            // STEP 3: Fill if #neighbours == #missing
            if (GetNeighbourAmount() == TargetValue)
            {
                Debug.Log("Exactly amount of Neighbours to satisfy field " + X + "," + Y);
                FillAllNeighbours();
                return true;
            }

            return false;
        }

        public bool Step4_Overload()
        {
            // Skip already finished fields
            if (Finished) return false;

            // STEP 4: If adding one would overload field -> set red
            OverloadsField();

            return false;
        }

        public bool Step5_Underload()
        {
            foreach (Direction direction in Directions)
            {
                // sum neighbours of all other directions
                int sum = 0;
                foreach (Direction direction1 in Directions)
                {
                    if (direction1 == direction) continue;

                    var neighbours = Neighbours[direction1];
                    sum += neighbours.Length;
                }

                // if sum < target -> in this direction set difference
                if (sum < TargetValue)
                {
                    Debug.Log("field " + X + "," + Y + "has to have " + (TargetValue - sum) + " more blue in direction " + direction);
                    AddInDirection(direction, TargetValue - sum, true);
                }
            }

            return Singleton.ChangeMade;
        }

        public void UpdateField()
        {
            UpdateNeighbours();
            UpdateMissing();
            UpdatePossibleDirections();
        }

        #endregion PUBLIC METHODS

    }

    #endregion Types

    #endregion


    #region UNITY INSPECTOR SETTINGS

    [Header("Prefabs")]

    [SerializeField]
    private GameObject _inputFieldPrefab;

    [SerializeField]
    private GameObject _outputFieldPrefab;

    [Header("Panels")]

    [SerializeField]
    private GameObject _selectSizePanel;

    [SerializeField]
    private GameObject _inputParentPanel;

    [SerializeField]
    private GameObject _outputParentPanel;

    [Header("Buttons")]

    [SerializeField]
    private GameObject _solveButton;

    [SerializeField]
    private GameObject _backButton;

    [SerializeField]
    private GameObject _resetButton;

    [Header("Debug")]
    [SerializeField]
    private GameObject _debugButton;

    #endregion


    #region PRIVATE MEMBERS

    private bool _debugMode;

    public static Dictionary<FieldColor, Color> Colors = new Dictionary<FieldColor, Color>(3)
    {
        { FieldColor.Empty, Color.grey},
        { FieldColor.Blue, Color.blue},
        { FieldColor.Red, Color.red}
    };

    public static List<Direction> Directions = new List<Direction>(4)
    {
        Direction.Up,
        Direction.Down,
        Direction.Left,
        Direction.Right
    };

    public CalculationField[,] CalculationFields { private set; get; }

    private GameObject[,] _inputFields;

    private List<CalculationField> _fieldsToCalculate = new List<CalculationField>();

    public bool ChangeMade = true;

    #endregion


    #region UNITY METHODS

    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
    }

    private void Start()
    {
        _outputParentPanel.SetActive(false);
        _inputParentPanel.SetActive(false);

#if DEBUGMODE
        debugButton.SetActive(true);
#else
        _debugButton.SetActive(false);
#endif

        _solveButton.SetActive(false);
        _resetButton.SetActive(false);
        _backButton.SetActive(false);

    }

    private void Update()
    {
        if (!_debugMode) return;

        HandleInput();
    }

    #endregion UNITY METHODS


    #region PRIVATE METHODS

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ResetOutput();
            UpdateFields();
            Step1_Satisfied();
            GenerateOutputField();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ResetOutput();
            UpdateFields();
            Step2_SingleDirection();
            GenerateOutputField();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ResetOutput();
            UpdateFields();
            Step3_MatchNeigbourAmount();
            GenerateOutputField();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ResetOutput();
            UpdateFields();
            Step4_Overload();
            GenerateOutputField();
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ResetOutput();
            UpdateFields();
            Step5_Underload();
            GenerateOutputField();
        }
    }


    #region Solve Steps

    private bool Step1_Satisfied()
    {
        foreach (CalculationField field in _fieldsToCalculate)
        {

            if (field.Step1_Satisfied()) break;
        }


        return ChangeMade;
    }

    private bool Step2_SingleDirection()
    {
        foreach (CalculationField field in _fieldsToCalculate)
        {

            if (field.Step2_SingleDirection()) break;
        }

        return ChangeMade;
    }

    private bool Step3_MatchNeigbourAmount()
    {
        foreach (CalculationField field in _fieldsToCalculate)
        {

            if (field.Step3_MatchNeigbourAmount()) break;
        }

        return ChangeMade;
    }

    private bool Step4_Overload()
    {
        foreach (CalculationField field in _fieldsToCalculate)
        {

            if (field.Step4_Overload()) break;
        }

        return ChangeMade;
    }

    private void Step5_Underload()
    {
        foreach (CalculationField field in _fieldsToCalculate)
        {

            if (field.Step5_Underload()) break;
        }
    }

    private void Step6_FillHoles()
    {
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                CalculationField field = CalculationFields[x, y];

                if (field.TargetValue != -1 || field.Color != FieldColor.Empty) continue;

                field.UpdateField();

                // STEP 6: Set surrounded by red also to red
                bool fill = true;

                foreach (Direction direction in Directions)
                {
                    var neighbours = field.Neighbours[direction];

                    if (neighbours.Length == 0)
                    {
                        continue;
                    }

                    fill = false;
                    break;
                }

                if (!fill) continue;

                Debug.Log("Field " + field.X + "," + field.Y + " is surrounded by red -> setting red");
                field.Color = FieldColor.Red;
                field.Finished = true;
            }
        }
    }

    #endregion Solve Steps


    #region Helpers

    #region Fields

    private void ResetOutput()
    {
        foreach (Transform child in _outputParentPanel.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void GenerateOutputField()
    {
        _inputParentPanel.SetActive(false);
        _outputParentPanel.SetActive(true);

        float initialPositionX = -(Size - 1.0f) / 2.0f * 50.0f;
        float initialPositionY = (Size - 1.0f) / 2.0f * 50.0f;

        for (int y = 0; y < Size; y++)
        {
            float actualPositionY = initialPositionY - 50.0f * y;

            for (int x = 0; x < Size; x++)
            {
                CalculationField field = CalculationFields[x, y];

                float actualPositionX = initialPositionX + 50.0f * x;

                GameObject newInputPanel = Instantiate(_outputFieldPrefab, _outputParentPanel.transform);
                RectTransform rect = newInputPanel.GetComponent<RectTransform>();
                rect.localPosition = new Vector3(actualPositionX, actualPositionY, 0);

                if (field.TargetValue != -1)
                {
                    Text text = newInputPanel.GetComponentInChildren<Text>();
                    text.text = field.TargetValue.ToString();
                }

                Image image = newInputPanel.GetComponent<Image>();
                image.color = Colors[field.Color];
            }
        }
    }

    private void GenerateCalculationField()
    {
        CalculationFields = new CalculationField[Size, Size];

        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var actualInputField = _inputFields[x, y];
                var actualInputValue = actualInputField.GetComponentInChildren<InputField>().text;

                switch (actualInputValue)
                {
                    case "r":
                        CalculationFields[x, y] = new CalculationField(-1, FieldColor.Red);
                        break;

                    case "":
                        CalculationFields[x, y] = new CalculationField(-1, FieldColor.Empty);
                        break;

                    default:
                        int val;

                        int.TryParse(actualInputValue, out val);

                        CalculationFields[x, y] = new CalculationField(val, FieldColor.Blue);
                        break;
                }

                if (x == 0)
                {
                    CalculationFields[x, y].PossibleDirections[Direction.Left] = false;
                }

                if (x == Size - 1)
                {
                    CalculationFields[x, y].PossibleDirections[Direction.Right] = false;
                }

                if (y == 0)
                {
                    CalculationFields[x, y].PossibleDirections[Direction.Up] = false;
                }

                if (y == Size - 1)
                {
                    CalculationFields[x, y].PossibleDirections[Direction.Down] = false;
                }

                // Tell fields their coordinates
                CalculationFields[x, y].X = x;
                CalculationFields[x, y].Y = y;

                if (CalculationFields[x, y].TargetValue != -1)
                {
                    _fieldsToCalculate.Add(CalculationFields[x, y]);
                }
            }
        }
    }

    private void ResetCalculationField()
    {
        CalculationFields = null;
        _fieldsToCalculate = new List<CalculationField>();
    }

    private void GenerateInputField(int size)
    {
        Size = size;
        _selectSizePanel.SetActive(false);
        _inputParentPanel.SetActive(true);

        _inputFields = new GameObject[Size, Size];

        float initialPositionX = -(Size - 1.0f) / 2.0f * 50.0f;
        float initialPositionY = (Size - 1.0f) / 2.0f * 50.0f;

        for (int y = 0; y < Size; y++)
        {
            float actualPositionY = initialPositionY - 50.0f * y;

            for (int x = 0; x < Size; x++)
            {
                float actualPositionX = initialPositionX + 50.0f * x;

                GameObject newInputPanel = Instantiate(_inputFieldPrefab, _inputParentPanel.transform);
                RectTransform rect = newInputPanel.GetComponent<RectTransform>();
                rect.localPosition = new Vector3(actualPositionX, actualPositionY, 0);

                _inputFields[x, y] = newInputPanel;
                _inputFields[x, y].GetComponentInChildren<InputField>().placeholder.GetComponent<Text>().text = x + "," + y;
            }
        }

    }

    private void ResetInputField()
    {
        foreach (Transform child in _inputParentPanel.transform)
        {
            Destroy(child.gameObject);
        }

        _inputParentPanel.SetActive(false);
        _selectSizePanel.SetActive(true);
    }

    #endregion Fields


    private void UpdateFields()
    {
        foreach (CalculationField field in _fieldsToCalculate)
        {
            field.UpdateField();
        }
    }

    private bool HeathCheck()
    {
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                CalculationField field = CalculationFields[x, y];

                if (field.TargetValue == -1) continue;

                // Check if field sees more than allowed
                int sum = 0;
                foreach (Direction direction in Directions)
                {
                    var neighbours = field.Neighbours[direction];
                    foreach (CalculationField neighbour in neighbours)
                    {
                        if (neighbour.Color == FieldColor.Blue)
                        {
                            sum++;
                        }

                        if (neighbour.Color == FieldColor.Empty) break;
                    }
                }
                if (sum > field.TargetValue)
                {
                    Debug.LogError("Error: Last Step overloaded field " + field.X + "," + field.Y);
                    return false;
                }

                // Check if less neighbours than needed
                sum = 0;
                foreach (Direction direction in Directions)
                {
                    var neighbours = field.Neighbours[direction];
                    sum += neighbours.Length;
                }

                if (sum < field.TargetValue)
                {
                    Debug.LogError("Error: Last Step underloaded field " + field.X + "," + field.Y);
                    return false;
                }
            }
        }

        return true;
    }

    #endregion Helpers

    #endregion PRIVATE METHODS


    #region PUBLIC METHODS

    public void Back()
    {
        _backButton.SetActive(false);
        _solveButton.SetActive(true);

        ResetCalculationField();

        _outputParentPanel.SetActive(false);
        _inputParentPanel.SetActive(true);

    }

    public void ShowInputField(int size)
    {
        Size = size;
        GenerateInputField(Size);

        _solveButton.SetActive(true);
        _resetButton.SetActive(true);
    }

    public void UseDebugField()
    {
        //int size = 4;
        //string[,] debug = new string[4, 4]
        //{
        //    {"2", "", "4", ""},
        //    {"", "", "3", "1"},
        //    {"", "", "4", ""},
        //    {"", "2", "", ""}
        //};

        const int size = 8;
        var debug = new[,]
        {
            { "","2","","","","","","8"},
            { "5","","","","","6","5",""},
            { "","","","r","r","","5",""},
            { "","","4","","3","","",""},
            { "","","","6","","","",""},
            { "2","4","","","","","","r"},
            { "","","5","7","","","",""},
            { "","","","","5","5","",""}
        };

        GenerateInputField(size);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                _inputFields[y, x].GetComponentInChildren<InputField>().text = debug[x, y];
            }
        }

        GenerateCalculationField();

        _debugMode = true;
    }

    public void Solve()
    {
        var st = new Stopwatch();
        st.Start();


        _solveButton.SetActive(false);
        _resetButton.SetActive(true);
        _backButton.SetActive(true);

        GenerateCalculationField();

        ChangeMade = true;

        while (ChangeMade)
        {
            ChangeMade = false;

            UpdateFields();

            if (!HeathCheck()) break;

            if (Step1_Satisfied()) continue;

            if (Step2_SingleDirection()) continue;

            if (Step3_MatchNeigbourAmount()) continue;

            if (Step4_Overload()) continue;

            Step5_Underload();
        }

        Step6_FillHoles();

        Debug.Log("No more founds. Generate state to output.");
        GenerateOutputField();

        st.Stop();
        Debug.Log(string.Format("Solving took {0} ms!", st.ElapsedMilliseconds));
    }

    public void Reset()
    {
        ResetCalculationField();
        ResetOutput();
        ResetInputField();

        _resetButton.SetActive(false);
        _solveButton.SetActive(false);
        _backButton.SetActive(false);

        _selectSizePanel.SetActive(true);
        _outputParentPanel.SetActive(false);
        _inputParentPanel.SetActive(false);
    }

    #endregion PUBLIC METHODS
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FieldManager : MonoBehaviour
{

    #region PUBLIC MEMBERS

    public static FieldManager Singleton;

    #region Types

    public enum FieldColor
    {
        Empty,
        Blue,
        Red
    }

    public enum Direction
    {
        up,
        down,
        left,
        right
    }

    public class CalculationField
    {
        public bool Finished = false;

        public int X;
        public int Y;

        public FieldColor Color;
        public int TargetValue;

        public int Missing { set; get; }

        public Dictionary<Direction, bool> possibleDirections = new Dictionary<Direction, bool>(4)
        {
            { Direction.up, true},
            { Direction.down, true},
            { Direction.left, true},
            { Direction.right, true}
        };

        public Dictionary<Direction, CalculationField[]> neighbours = new Dictionary<Direction, CalculationField[]>(4)
        {
            { Direction.up, null},
            { Direction.down, null},
            { Direction.left, null},
            { Direction.right, null}
        };

        public CalculationField(int initialTargetValue, FieldColor initialColor)
        {
            TargetValue = initialTargetValue;
            Color = initialColor;
        }
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

    private bool debugMode = false;

    private List<FieldColor> fieldColors = new List<FieldColor>(3)
    {
        FieldColor.Empty,
        FieldColor.Blue,
        FieldColor.Red
    };

    private Dictionary<FieldColor, Color> colors = new Dictionary<FieldColor, Color>(3)
    {
        { FieldColor.Empty, Color.grey},
        { FieldColor.Blue, Color.blue},
        { FieldColor.Red, Color.red}
    };

    private List<Direction> directions = new List<Direction>(4)
    {
        Direction.up,
        Direction.down,
        Direction.left,
        Direction.right
    };

    private CalculationField[,] calculationFields;

    private GameObject[,] inputFields;

    private int _size;

    private bool changeMade = true;

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
        if (!debugMode) return;

        HandleInput();
    }

    #endregion UNITY METHODS


    #region PRIVATE METHODS

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ResetOutput();
            SetNeighbourFields();
            Step1_Satisfied();
            GenerateOutputField();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ResetOutput();
            SetNeighbourFields();
            Step2_SingleDirection();
            GenerateOutputField();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ResetOutput();
            SetNeighbourFields();
            Step3_MatchNeigbourAmount();
            GenerateOutputField();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ResetOutput();
            SetNeighbourFields();
            Step4_Overload();
            GenerateOutputField();
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ResetOutput();
            SetNeighbourFields();
            Step5_Underload();
            GenerateOutputField();
        }
    }



    #region Solve Steps

    private bool Step1_Satisfied()
    {
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
            {
                CalculationField field = calculationFields[x, y];

                // Skip fields which are not set
                if (field.TargetValue == -1) continue;

                // Skip already finished fields
                if (field.Finished) continue;

                if (Satisfied(field))
                {
                    Debug.Log("Field " + field.X + "," + field.Y + " is satisfied.");
                    foreach (Direction direction in directions)
                    {
                        BlockEndDirection(field, direction);
                    }
                    field.Finished = true;
                    changeMade = true;
                    return true;
                }
            }
        }

        return false;
    }

    private bool Step2_SingleDirection()
    {
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
            {
                CalculationField field = calculationFields[x, y];

                // Skip fields which are not set
                if (field.TargetValue == -1) continue;

                // Skip already finished fields
                if (field.Finished) continue;

                CalculateMissing(field);

                if (field.Missing == 0) continue;

                // STEP 2: Fill if only 1 direction
                CalculatePossibleDirections(field);

                if (OnlyOneDirection(field))
                {
                    Debug.Log("Only one direction for field " + field.X + "," + field.Y);
                    Direction dir = GetOnlyDirection(field);
                    Debug.Log(dir);

                    AddInDirection(field, dir, 1, false);
                    return true;
                }
            }
        }

        return false;
    }

    private bool Step3_MatchNeigbourAmount()
    {
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
            {
                //TODO DEBUG
                if (x == 5 && y == 8)
                {
                    Debug.Log("helo");
                }
                CalculationField field = calculationFields[x, y];

                // Skip fields which are not set
                if (field.TargetValue == -1) continue;

                // Skip already finished fields
                if (field.Finished) continue;

                // STEP 3: Fill if #neighbours == #missing
                if (GetNeighbourAmount(field) == field.TargetValue)
                {
                    Debug.Log("Exactly amount of Neighbours to satisfy field " + field.X + "," + field.Y);
                    FillAllNeighbours(field);
                    return true;
                }
            }
        }

        return false;
    }

    private bool Step4_Overload()
    {
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
            {
                CalculationField field = calculationFields[x, y];

                // Skip fields which are not set
                if (field.TargetValue == -1) continue;

                // Skip already finished fields
                if (field.Finished) continue;

                // STEP 4: If adding one would overload field -> set red
                OverloadsField(field);
            }
        }

        return false;
    }

    private bool Step5_Underload()
    {
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
            {
                CalculationField field = calculationFields[x, y];

                foreach (Direction direction in directions)
                {
                    // sum neighbours of all other directions
                    int sum = 0;
                    foreach (Direction direction1 in directions)
                    {
                        if (direction1 == direction) continue;

                        var neighbours = field.neighbours[direction1];
                        sum += neighbours.Length;
                    }

                    // if sum < target -> in this direction set difference
                    if (sum < field.TargetValue)
                    {
                        Debug.Log("field " + field.X + "," + field.Y + "has to have " + (field.TargetValue - sum) +
                                  " more blue in direction " + direction);
                        AddInDirection(field, direction, field.TargetValue - sum, true);
                    }
                }
            }
        }

        return changeMade;
    }

    private void Step6_FillHoles()
    {
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
            {
                CalculationField field = calculationFields[x, y];

                if (field.TargetValue == -1 && field.Color == FieldColor.Empty)
                {
                    // STEP 0: Set surrounded by red also to red
                    bool fill = true;

                    SetNeighbourFields();

                    foreach (Direction direction in directions)
                    {
                        var neighbours = field.neighbours[direction];
                        CalculationField neighbour = null;
                        if (neighbours.Length != 0)
                        {
                            neighbour = neighbours[0];
                        }

                        if (neighbour == null || neighbour.Color == FieldColor.Red)
                        {
                            continue;
                        }

                        fill = false;
                    }

                    if (fill)
                    {
                        Debug.Log("Field " + field.X + "," + field.Y + " is surrounded by red -> setting red");
                        field.Color = FieldColor.Red;
                        field.Finished = true;
                    }
                }
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

        float initialPositionX = -(_size - 1.0f) / 2.0f * 50.0f;
        float initialPositionY = (_size - 1.0f) / 2.0f * 50.0f;

        for (int y = 0; y < _size; y++)
        {
            float actualPositionY = initialPositionY - 50.0f * y;

            for (int x = 0; x < _size; x++)
            {
                CalculationField field = calculationFields[x, y];

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
                image.color = colors[field.Color];
            }
        }
    }

    private void GenerateCalculationField()
    {
        calculationFields = new CalculationField[_size, _size];

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                GameObject actualInputField = inputFields[x, y];
                string actualInputValue = actualInputField.GetComponentInChildren<InputField>().text;

                switch (actualInputValue)
                {
                    case "r":
                        calculationFields[x, y] = new CalculationField(-1, FieldColor.Red);
                        break;

                    case "":
                        calculationFields[x, y] = new CalculationField(-1, FieldColor.Empty);
                        break;

                    default:
                        int val = 0;

                        Int32.TryParse(actualInputValue, out val);

                        calculationFields[x, y] = new CalculationField(val, FieldColor.Blue);
                        break;
                }

                if (x == 0)
                {
                    calculationFields[x, y].possibleDirections[Direction.left] = false;
                }

                if (x == _size - 1)
                {
                    calculationFields[x, y].possibleDirections[Direction.right] = false;
                }

                if (y == 0)
                {
                    calculationFields[x, y].possibleDirections[Direction.up] = false;
                }

                if (y == _size - 1)
                {
                    calculationFields[x, y].possibleDirections[Direction.down] = false;
                }

                //DEBUG
                calculationFields[x, y].X = x;
                calculationFields[x, y].Y = y;
            }
        }
    }

    private void ResetCalculationField()
    {
        calculationFields = null;
    }

    private void GenerateInputField(int size)
    {
        _size = size;
        _selectSizePanel.SetActive(false);
        _inputParentPanel.SetActive(true);

        inputFields = new GameObject[_size, _size];

        float initialPositionX = -(_size - 1.0f) / 2.0f * 50.0f;
        float initialPositionY = (_size - 1.0f) / 2.0f * 50.0f;

        for (int y = 0; y < _size; y++)
        {
            float actualPositionY = initialPositionY - 50.0f * y;

            for (int x = 0; x < _size; x++)
            {
                float actualPositionX = initialPositionX + 50.0f * x;

                GameObject newInputPanel = Instantiate(_inputFieldPrefab, _inputParentPanel.transform);
                RectTransform rect = newInputPanel.GetComponent<RectTransform>();
                rect.localPosition = new Vector3(actualPositionX, actualPositionY, 0);

                inputFields[x, y] = newInputPanel;
                inputFields[x, y].GetComponentInChildren<InputField>().placeholder.GetComponent<Text>().text = x + "," + y;
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

    private void OverloadsField(CalculationField field)
    {
        foreach (Direction direction in directions)
        {
            if (!field.possibleDirections[direction]) continue;

            // get first empty neighbour in direction
            var neighbours = field.neighbours[direction];
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
            CalculateMissing(field);

            // if missing < 0 -> set neighbour red; continue;
            if (field.Missing < 0)
            {
                Debug.Log(emptyNeighbour.X + "," + emptyNeighbour.Y + " would overload field " + field.X + "," + field.Y);
                Debug.Log("---Setting field " + emptyNeighbour.X + "," + emptyNeighbour.Y + " to red.");
                emptyNeighbour.Color = FieldColor.Red;
                changeMade = true;
                return;
            }

            // set nieghbour empty
            emptyNeighbour.Color = FieldColor.Empty;
        }
    }

    private void FillAllNeighbours(CalculationField field)
    {
        foreach (Direction direction in directions)
        {
            var neighbours = field.neighbours[direction];
            foreach (CalculationField neighbour in neighbours)
            {
                if (neighbour.TargetValue != -1) continue;

                if (neighbour.Color != FieldColor.Empty) continue;

                neighbour.Color = FieldColor.Blue;
                neighbour.Finished = true;
                changeMade = true;
            }
        }

        field.Finished = true;
        changeMade = true;
    }

    private bool OnlyTwoDirections(CalculationField field)
    {
        int count = 0;

        foreach (Direction direction in directions)
        {
            if (field.possibleDirections[direction])
            {
                count++;
            }
        }

        return count == 2;
    }

    private bool OnlyOneDirection(CalculationField field)
    {
        int count = 0;

        foreach (Direction direction in directions)
        {
            if (field.possibleDirections[direction])
            {
                count++;
            }
        }

        return count == 1;
    }

    private int GetNeighbourAmount(CalculationField field)
    {
        int ret = 0;
        CalculateNeighbours(field);

        foreach (Direction direction in directions)
        {
            var neighbours = field.neighbours[direction];

            ret += neighbours.Length;
        }

        return ret;
    }

    private Direction GetOnlyDirection(CalculationField field)
    {
        Direction ret = Direction.up;

        foreach (Direction direction in directions)
        {
            if (field.possibleDirections[direction])
            {
                return direction;
            }
        }

        Debug.LogError("No Direction is possible for field " + field.X + "," + field.Y);
        return ret;
    }

    private bool Satisfied(CalculationField field)
    {
        CalculateMissing(field);
        if (field.Missing == 0)
        {
            return true;
        }

        return false;
    }

    private CalculationField[] NeighboursInDirection(CalculationField field, Direction direction)
    {
        List<CalculationField> neighbours = new List<CalculationField>();

        int actX = field.X;
        int actY = field.Y;

        switch (direction)
        {
            case Direction.up:
                for (int i = actY - 1; i >= 0; i--)
                {
                    CalculationField nextNeighbour = calculationFields[actX, i];

                    if (nextNeighbour.Color == FieldColor.Red) break;

                    neighbours.Add(nextNeighbour);
                }
                break;

            case Direction.down:
                for (int i = actY + 1; i < _size; i++)
                {
                    CalculationField nextNeighbour = calculationFields[actX, i];
                    if (nextNeighbour.Color == FieldColor.Red) break;

                    neighbours.Add(nextNeighbour);
                }
                break;

            case Direction.left:
                for (int i = actX - 1; i >= 0; i--)
                {
                    CalculationField nextNeighbour = calculationFields[i, actY];
                    if (nextNeighbour.Color == FieldColor.Red) break;

                    neighbours.Add(nextNeighbour);
                }
                break;

            case Direction.right:
                for (int i = actX + 1; i < _size; i++)
                {
                    CalculationField nextNeighbour = calculationFields[i, actY];
                    if (nextNeighbour.Color == FieldColor.Red) break;

                    neighbours.Add(nextNeighbour);
                }
                break;
        }

        return neighbours.ToArray();
    }

    private void CalculatePossibleDirections(CalculationField field)
    {
        CalculateNeighbours(field);

        foreach (Direction direction in directions)
        {
            // Skip already blocked directions
            if (!field.possibleDirections[direction]) continue;

            var neighbours = field.neighbours[direction];

            if (neighbours.Length == 0)
            {
                // blocked in this direction
                field.possibleDirections[direction] = false;
                changeMade = true;
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
                field.possibleDirections[direction] = false;
                changeMade = true;
            }
        }
    }

    private void CalculateMissing(CalculationField field)
    {
        int actualCount = 0;

        CalculateNeighbours(field);

        foreach (Direction direction in directions)
        {
            var neighbours = field.neighbours[direction];
            foreach (CalculationField neighbourField in neighbours)
            {
                if (neighbourField.Color != FieldColor.Blue) break;

                actualCount++;
            }
        }

        field.Missing = field.TargetValue - actualCount;
    }

    private void AddInDirection(CalculationField field, Direction direction, int amount, bool goOn)
    {
        int targetAmount = amount;
        CalculateNeighbours(field);

        var neighbours = field.neighbours[direction];
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
                    changeMade = true;
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

    private void BlockEndDirection(CalculationField field, Direction direction)
    {
        CalculateNeighbours(field);

        // Skip direction if it is already blocked
        if (!field.possibleDirections[direction]) return;

        var neighbours = field.neighbours[direction];

        if (neighbours.Length == 0)
        {
            field.possibleDirections[direction] = false;
            changeMade = true;
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
                    field.possibleDirections[direction] = false;
                    Debug.Log("---Blocking in direction " + direction);
                    changeMade = true;
                    return;
            }
        }

        Debug.Log("---" + direction + " is blocked by field limit.");
        field.possibleDirections[direction] = false;
        changeMade = true;
    }

    private void SetNeighbourFields()
    {
        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                CalculationField field = calculationFields[x, y];
                CalculateNeighbours(field);
            }
        }
    }

    private void CalculateNeighbours(CalculationField field)
    {
        foreach (Direction direction in directions)
        {
            field.neighbours[direction] = NeighboursInDirection(field, direction);
        }
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
        _size = size;
        GenerateInputField(_size);

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

        int size = 8;
        string[,] debug = new string[8, 8]
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

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                inputFields[y, x].GetComponentInChildren<InputField>().text = debug[x, y];
            }
        }

        GenerateCalculationField();

        debugMode = true;
    }

    private bool HeathCheck()
    {
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
            {
                CalculationField field = calculationFields[x, y];

                if (field.TargetValue == -1) continue;

                // Check if field sees more than allowed
                int sum = 0;
                foreach (Direction direction in directions)
                {
                    var neighbours = field.neighbours[direction];
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
                foreach (Direction direction in directions)
                {
                    var neighbours = field.neighbours[direction];
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

    public void Solve()
    {
        _solveButton.SetActive(false);
        _resetButton.SetActive(true);
        _backButton.SetActive(true);

        GenerateCalculationField();

        changeMade = true;

        while (changeMade)
        {
            changeMade = false;

            SetNeighbourFields();
            // TODO do magic

            if (!HeathCheck()) break;

            if (Step1_Satisfied()) continue;

            if (Step2_SingleDirection()) continue;

            if (Step3_MatchNeigbourAmount()) continue;

            if (Step4_Overload()) continue;

            if (Step5_Underload()) continue;
        }

        Step6_FillHoles();

        Debug.Log("No more founds. Generate state to output.");
        GenerateOutputField();
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

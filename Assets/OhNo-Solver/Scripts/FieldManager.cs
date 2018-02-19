using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.UI;

public class FieldManager : MonoBehaviour
{

    [SerializeField]
    private GameObject inputFieldPrefab;

    [SerializeField]
    private GameObject outputFieldPrefab;


    [SerializeField]
    private GameObject selectSizePanel;

    [SerializeField]
    private GameObject uiParentPanel;


    [SerializeField]
    private GameObject outputParentObject;

    public static FieldManager Singleton;

    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
    }

    private void Start()
    {
        outputParentObject.SetActive(false);
        uiParentPanel.SetActive(false);
    }


    public enum FieldColor
    {
        Empty,
        Blue,
        Red
    }

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

    public enum Direction
    {
        up,
        down,
        left,
        right
    }

    private List<Direction> directions = new List<Direction>(4)
    {
        Direction.up,
        Direction.down,
        Direction.left,
        Direction.right
    };

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

    private CalculationField[,] calculationFields;

    private GameObject[,] inputFields;

    private int _size;

    public void GenerateInputField(int size)
    {
        _size = size;
        selectSizePanel.SetActive(false);
        uiParentPanel.SetActive(true);

        inputFields = new GameObject[_size, _size];

        float initialPositionX = -(_size - 1.0f) / 2.0f * 50.0f;
        float initialPositionY = (_size - 1.0f) / 2.0f * 50.0f;

        for (int y = 0; y < _size; y++)
        {
            float actualPositionY = initialPositionY - 50.0f * y;

            for (int x = 0; x < _size; x++)
            {
                float actualPositionX = initialPositionX + 50.0f * x;

                GameObject newInputPanel = Instantiate(inputFieldPrefab, uiParentPanel.transform);
                RectTransform rect = newInputPanel.GetComponent<RectTransform>();
                rect.localPosition = new Vector3(actualPositionX, actualPositionY, 0);

                inputFields[x, y] = newInputPanel;
                inputFields[x, y].GetComponentInChildren<InputField>().placeholder.GetComponent<Text>().text =
                    x + "," + y;
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

    private void GenerateOutputField()
    {
        uiParentPanel.SetActive(false);
        outputParentObject.SetActive(true);

        float initialPositionX = -(_size - 1.0f) / 2.0f * 50.0f;
        float initialPositionY = (_size - 1.0f) / 2.0f * 50.0f;

        for (int y = 0; y < _size; y++)
        {
            float actualPositionY = initialPositionY - 50.0f * y;

            for (int x = 0; x < _size; x++)
            {
                CalculationField field = calculationFields[x, y];

                float actualPositionX = initialPositionX + 50.0f * x;

                GameObject newInputPanel = Instantiate(outputFieldPrefab, outputParentObject.transform);
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

    private bool changeMade = true;

    public void Solve()
    {
        GenerateCalculationField();

        while (changeMade)
        {
            changeMade = false;

            SetNeighbourFields();
            // TODO do magic
            for (int x = 0; x < _size; x++)
            {
                for (int y = 0; y < _size; y++)
                {
                    CalculationField field = calculationFields[x, y];

                    // Skip fields which are not set
                    if (field.TargetValue == -1) continue;

                    if (Satisfied(field))
                    {
                        Debug.Log("Field " + field.X + "," + field.Y + " is satisfied.");
                        foreach (Direction direction in directions)
                        {
                            BlockEndDirection(field, direction);
                        }
                    }

                    CalculatePossibleDirections(field);

                    if (OnlyOneDirection(field))
                    {
                        Direction dir = GetOnlyDirection(field);
                        int missing = field.Missing;

                        AddInDirection(field, dir, missing);
                    }
                }
            }
        }

        GenerateOutputField();
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
                    if (nextNeighbour.Color == FieldColor.Red)
                    {
                        field.possibleDirections[Direction.up] = false;
                        break;
                    }

                    neighbours.Add(nextNeighbour);
                }
                break;

            case Direction.down:
                for (int i = actY + 1; i < _size; i++)
                {
                    CalculationField nextNeighbour = calculationFields[actX, i];
                    if (nextNeighbour.Color == FieldColor.Red)
                    {
                        field.possibleDirections[Direction.down] = false;
                        break;
                    }
                    neighbours.Add(nextNeighbour);
                }
                break;

            case Direction.left:
                for (int i = actX + 1; i < _size; i++)
                {
                    CalculationField nextNeighbour = calculationFields[i, actY];
                    if (nextNeighbour.Color == FieldColor.Red)
                    {
                        field.possibleDirections[Direction.left] = false;
                        break;
                    }
                    neighbours.Add(nextNeighbour);
                }
                break;

            case Direction.right:
                for (int i = actX - 1; i >= 0; i--)
                {
                    CalculationField nextNeighbour = calculationFields[i, actY];
                    if (nextNeighbour.Color == FieldColor.Red)
                    {
                        field.possibleDirections[Direction.right] = false;
                        break;
                    }
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
            if (!field.possibleDirections[direction]) break;

            var neighbours = field.neighbours[direction];

            if (neighbours.Length == 0)
            {
                // blocked in this direction
                field.possibleDirections[direction] = false;
                break;
            }

            foreach (CalculationField neighbour in neighbours)
            {
                if (neighbour.Color == FieldColor.Blue) continue;

                if (neighbour.Color == FieldColor.Empty)
                {
                    // blocked in this direction
                    field.possibleDirections[direction] = false;
                    break;
                }
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

    private void AddInDirection(CalculationField field, Direction direction, int amount)
    {
        int targetAmount = amount;
        CalculateNeighbours(field);

        var neighbours = field.neighbours[direction];
        foreach (CalculationField neighbour in neighbours)
        {
            // Initially Set Fields can't be changed
            if (neighbour.TargetValue != -1) continue;

            switch (neighbour.Color)
            {
                case FieldColor.Empty:
                    // Set field to blue, reduce amount by one and go on to next field
                    neighbour.Color = FieldColor.Blue;
                    targetAmount--;
                    changeMade = true;
                    break;

                case FieldColor.Blue:
                    // Do nothing and go to next field
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
                    field.possibleDirections[direction] = false;
                    Debug.Log("Blocking in direction " + direction);
                    changeMade = true;
                    return;
            }
        }

        Debug.Log(direction + " is blocked by field limit.");
        field.possibleDirections[direction] = false;
        Debug.Log("Blocking in direction " + direction);
        changeMade = true;
    }

    public void UseDebugField()
    {
        string[,] debug = new string[4, 4]
        {
            {"2","","4","" },
            {"","","3","1" },
            {"","","4","" },
            {"","2","","" }
        };

        GenerateInputField(4);

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                inputFields[y, x].GetComponentInChildren<InputField>().text = debug[x, y];
            }
        }
    }
}

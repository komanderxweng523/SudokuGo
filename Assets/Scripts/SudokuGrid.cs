using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;

public class SudokuGrid : MonoBehaviour
{
    public int columns = 0;
    public int rows = 0;
    public float square_offset = 0.0f;
    public GameObject grid_square;
    public Vector2 start_position = new Vector2(0.0f, 0.0f);
    public float square_scale = 1.0f;
    public float square_gap = 0.1f;
    public Color line_highlight_color = Color.red;

    public static SudokuData.SudokuBoardData unsolvedBoard;
    private static List<GameObject> grid_squares_;

    // Start is called before the first frame update
    void Start()
    {
        if(grid_square.GetComponent<GridSquare>() == null)
            Debug.LogError("This GameObject need to have GridSquare script attached!");

        grid_squares_ = new List<GameObject>();

        CreateGrid();
        SetGridNumber();
    }

    // calls functions to create empty grid
    private void CreateGrid()
    {
        SpawnGridSquares();
        SetSquarePosition();
    }

    private void SpawnGridSquares()
    {
        int square_index = 0;
        for(int row = 0; row < rows; row++)
        {
            for(int column = 0; column < columns; column++)
            {
                grid_squares_.Add(Instantiate(grid_square) as GameObject);
                grid_squares_[grid_squares_.Count -1].GetComponent<GridSquare>().SetSquareIndex(square_index);
                grid_squares_[grid_squares_.Count -1].transform.parent = this.transform;
                grid_squares_[grid_squares_.Count -1].transform.localScale = new Vector3(square_scale, square_scale, square_scale);

                square_index++;
            }
        }
    }

    private void SetSquarePosition()
    {
        var square_rect = grid_squares_[0].GetComponent<RectTransform>();
        Vector2 offset = new Vector2();
        Vector2 square_gap_number = new Vector2(0.0f, 0.0f);
        bool row_moved = false;

        offset.x = square_rect.rect.width * square_rect.transform.localScale.x + square_offset;
        offset.y = square_rect.rect.height * square_rect.transform.localScale.y + square_offset;

        int column_number = 0;
        int row_number = 0;

        foreach(GameObject square in grid_squares_)
        {
            if(column_number + 1 > columns)
            {
                row_number++;
                column_number = 0;
                square_gap_number.x = 0;
                row_moved = false;
            }

            var pos_x_offset = offset.x * column_number + (square_gap_number.x * square_gap);
            var pos_y_offset = offset.y * row_number + (square_gap_number.y * square_gap);

            if(column_number > 0 && column_number % 3 == 0)
            {
                square_gap_number.x++;
                pos_x_offset += square_gap;
            }

            if(row_number > 0 && row_number % 3 == 0 && row_moved == false)
            {
                row_moved = true;
                square_gap_number.y++;
                pos_y_offset += square_gap;
            }
            square.GetComponent<RectTransform>().anchoredPosition = new Vector2(start_position.x + pos_x_offset, start_position.y - pos_y_offset);
            column_number++;
        }
    }

    private void SetGridNumber()
    {
        var data = SudokuGameData.getData();

        SetGridSquareData(data);
    }

    private void SetGridSquareData(SudokuData.SudokuBoardData data)
    {
        unsolvedBoard = data;
        for (int i = 0; i < grid_squares_.Count; i++)
        {
            grid_squares_[i].GetComponent<GridSquare>().SetNumber(data.unsolvedData[i]);
            grid_squares_[i].GetComponent<GridSquare>().SetHasDefaultValue(data.unsolvedData[i] != 0);
        }
    }

    public static int[] getCurrentGrid()
    {
        int n = SudokuData.N;
        int[] output = new int[n*n];

        for (int i = 0; i < grid_squares_.Count; i++)
        {
            output[i] = grid_squares_[i].GetComponent<GridSquare>().GetNum();
        }

        return output;
    }

    private void OnEnable()
    {
        GameEvents.OnSquareSelected += OnSquareSelected;
    }

    private void OnDisable()
    {
        GameEvents.OnSquareSelected -= OnSquareSelected;
    }

    public void OnSquareSelected(int square_index)
    {
        var horizontal_line = LineIndicator.instance.GetHorizontalLine(square_index);
        var vertical_line = LineIndicator.instance.GetVerticalLine(square_index);
        var square = LineIndicator.instance.GetSquare(square_index);

        if(grid_squares_[square_index].GetComponent<GridSquare>().GetHasDefaultValue() == false)
        {
            SetSquareColor(LineIndicator.instance.GetAllSquaresIndexes(), Color.white);

            SetSquareColor(horizontal_line, line_highlight_color);
            SetSquareColor(vertical_line, line_highlight_color);
            SetSquareColor(square, line_highlight_color);
        }
        else
        {
            foreach(var gridSquare in grid_squares_)
            {
                var comp = gridSquare.GetComponent<GridSquare>();
                if(comp.HasWrongValue() == false && comp.IsSelected() == false)
                    comp.SetSquareColor(Color.white);
            }
        }
        
    }

    private void SetSquareColor(int[] data, Color col)
    {
        foreach(var index in data)
        {
            var comp = grid_squares_[index].GetComponent<GridSquare>();
            if(comp.HasWrongValue() == false && comp.IsSelected() == false)
            {
                comp.SetSquareColor(col);
            }
        }
    }
}

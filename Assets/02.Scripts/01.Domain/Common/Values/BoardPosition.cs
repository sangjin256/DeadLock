using System;

public readonly struct BoardPosition : IEquatable<BoardPosition>
{
    public static readonly BoardPosition Zero = new BoardPosition(0, 0);

    public readonly int Row;
    public readonly int Column;

    public BoardPosition(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public int GetManhattanDistance(BoardPosition other)
    {
        return Math.Abs(Row - other.Row) + Math.Abs(Column - other.Column);
    }

    public bool Equals(BoardPosition other)
    {
        return Row == other.Row && Column == other.Column;
    }

    public override bool Equals(object obj)
    {
        return obj is BoardPosition other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Row * 397) ^ Column;
        }
    }

    public static bool operator ==(BoardPosition left, BoardPosition right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BoardPosition left, BoardPosition right)
    {
        return !left.Equals(right);
    }
}

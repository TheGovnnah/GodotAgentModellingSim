public struct CellUpdate
{
    
    public Cell oldCell;
    public Cell newCell;
    public Agent agent;

    public CellUpdate(Cell oldCell, Cell newCell, Agent agent)
    {
        this.oldCell = oldCell;
        this.newCell = newCell;
        this.agent = agent;
    }
}
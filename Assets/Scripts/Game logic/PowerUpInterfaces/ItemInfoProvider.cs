using System.Collections.Generic;
using UnityEngine;

public interface ItemInfoProvider 
{
    public List<Item> GetActiveItems { get; }
    public void AddOrRemoveItem(Item item, bool add);


}

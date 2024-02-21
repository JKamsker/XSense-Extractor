using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XSense.Models.Init;

namespace XSense.Models.Aggregates;

public class HouseDetailAggregate
{
    private readonly House _house;
    private readonly HouseDetail _detail;

    public string Name => _house.HouseName;
    public string HouseId => _house.HouseId;

    public Station[] Stations => _detail.Stations;

    public HouseDetailAggregate(House house, HouseDetail detail)
    {
        _house = house;
        _detail = detail;
    }
}
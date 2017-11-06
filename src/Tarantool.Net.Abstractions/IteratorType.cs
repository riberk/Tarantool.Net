namespace Tarantool.Net.Abstractions
{
    ///<summary>
    /// <see href="https://github.com/tarantool/tarantool/blob/1.7/src/box/iterator_type.h">
    /// tarantool sources
    /// </see>
    ///</summary>
    public enum IteratorType
    {
        ///<summary>key == x ASC order</summary>
        Eq = 0,

        ///<summary>key == x DESC order</summary>
        Req = 1,

        ///<summary>all tuples</summary>
        All = 2,

        ///<summary>key &lt; x</summary>
        Lt = 3,

        ///<summary>key &lt;= x</summary>
        Le = 4,

        ///<summary>key &gt;= x</summary>
        Ge = 5,

        ///<summary>key &gt; x</summary>
        Gt = 6,

        ///<summary>all bits from x are set in key</summary>
        BitsAllSet = 7,

        ///<summary>at least one x's bit is set</summary>
        BitsAnySet = 8,

        ///<summary>all bits are not set</summary>
        BitsAllNotSet = 9,

        ///<summary>key overlaps x</summary>
        Overlaps = 10,

        ///<summary>tuples in distance ascending order from specified point</summary>
        Neighbor = 11,
    }
}
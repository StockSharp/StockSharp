import clr

# Add references to required assemblies
clr.AddReference("NumpyDotNet")

from System import Array, Double

from NumpyDotNet import np, ndarray
from NumpyDotNet import NumpyExtensions as npext

class nx:
    """
    Static class providing ndarray extension methods for IronPython.
    Wraps NumpyDotNet.NumpyExtensions functionality.
    """
    
    @staticmethod
    def reshape(arr, *newshape):
        """
        Returns an array containing the same data with a new shape.
        """
        return npext.reshape(arr, newshape)

    @staticmethod
    def tofile(arr, file_name, sep=None, format=None):
        """
        Write array to a file as text or binary.
        """
        npext.tofile(arr, file_name, sep, format)

    @staticmethod
    def view(arr, dtype=None, type=None):
        """
        New view of array with the same data.
        """
        return npext.view(arr, dtype, type)

    @staticmethod
    def flatten(arr, order='C'):
        """
        Return a copy of the array collapsed into one dimension.
        """
        return npext.Flatten(arr, order)

    @staticmethod
    def ravel(arr, order='C'):
        """
        Return a contiguous flattened array.
        """
        return npext.Ravel(arr, order)

    @staticmethod
    def resize(arr, newdims):
        """
        Change shape and size of array in-place.
        """
        npext.Resize(arr, newdims)

    @staticmethod
    def squeeze(arr):
        """
        Remove axes of length one from arr.
        """
        return npext.Squeeze(arr)

    @staticmethod
    def swapaxes(arr, axis1, axis2):
        """
        Interchange two axes of an array.
        """
        return npext.SwapAxes(arr, axis1, axis2)

    @staticmethod
    def transpose(arr, axes=None):
        """
        Returns a view of the array with axes transposed.
        """
        return npext.Transpose(arr, axes)

    @staticmethod
    def choose(arr, choices, out=None, mode='raise'):
        """
        Construct array from an index array and a list of arrays to choose from.
        """
        return npext.Choose(arr, choices, out, mode)

    @staticmethod
    def repeat(arr, repeats, axis=None):
        """
        Repeat elements of an array.
        """
        return npext.Repeat(arr, repeats, axis)

    @staticmethod
    def put(arr, indices, values, mode='raise'):
        """
        Replaces specified elements of an array with given values.
        """
        npext.PutTo(arr, values, indices, mode)

    @staticmethod
    def sort(arr, axis=-1, kind='quicksort'):
        """
        Sort an array in-place.
        """
        return npext.Sort(arr, axis, kind)

    @staticmethod
    def argsort(arr, axis=-1, kind='quicksort'):
        """
        Returns the indices that would sort an array.
        """
        return npext.ArgSort(arr, axis, kind)

    @staticmethod
    def argmax(arr, axis=None):
        """
        Returns the indices of the maximum values along an axis.
        """
        return npext.ArgMax(arr, axis)

    @staticmethod
    def argmin(arr, axis=None):
        """
        Returns the indices of the minimum values along an axis.
        """
        return npext.ArgMin(arr, axis)

    @staticmethod
    def searchsorted(arr, v, side='left'):
        """
        Find indices where elements should be inserted to maintain order.
        """
        return npext.SearchSorted(arr, v, side)

    @staticmethod
    def diagonal(arr, offset=0, axis1=0, axis2=1):
        """
        Return specified diagonals.
        """
        return npext.diagonal(arr, offset, axis1, axis2)

    @staticmethod
    def trace(arr, offset=0, axis1=0, axis2=1, dtype=None):
        """
        Return the sum along diagonals of the array.
        """
        return npext.trace(arr, offset, axis1, axis2, dtype)

    @staticmethod
    def nonzero(arr):
        """
        Return the indices of the elements that are non-zero.
        """
        return npext.NonZero(arr)

    @staticmethod
    def compress(arr, condition, axis=None, out=None):
        """
        Return selected slices of an array along given axis.
        """
        return npext.compress(arr, condition, axis, out)

    @staticmethod
    def clip(arr, a_min, a_max, out=None):
        """
        Clip (limit) the values in an array.
        """
        return npext.clip(arr, a_min, a_max, out)

    @staticmethod
    def sum(arr, axis=None, dtype=None, out=None):
        """
        Sum of array elements over a given axis.
        """
        return npext.Sum(arr, axis, dtype, out)

    @staticmethod
    def any(arr, axis=None, out=None, keepdims=False):
        """
        Test whether any array element along a given axis evaluates to True.
        """
        return npext.Any(arr, axis, out, keepdims)

    @staticmethod
    def anyb(arr, axis=None, out=None, keepdims=False):
        """
        Return bool result from np.any
        """
        return npext.Anyb(arr, axis, out, keepdims)

    @staticmethod
    def all(arr, axis=None, out=None, keepdims=False):
        """
        Test whether all array elements along a given axis evaluate to True.
        """
        return npext.All(arr, axis, out, keepdims)

    @staticmethod
    def cumsum(arr, axis=None, dtype=None, out=None):
        """
        Return the cumulative sum of the elements along a given axis.
        """
        return npext.cumsum(arr, axis, dtype, out)

    @staticmethod
    def mean(arr, axis=None, dtype=None):
        """
        Compute the arithmetic mean along the specified axis.
        """
        return npext.Mean(arr, axis, dtype)

    @staticmethod
    def std(arr, axis=None, dtype=None):
        """
        Compute the standard deviation along the specified axis.
        """
        return npext.Std(arr, axis, dtype)

    @staticmethod
    def tolist(arr):
        """
        Convert ndarray to Python list.
        """
        return npext.ToList[object](arr)

    @staticmethod
    def toarray(arr):
        """
        Convert ndarray to .NET array.
        """
        return npext.ToArray(arr)
        
    @staticmethod
    def tosystemarray(arr):
        """
        Convert ndarray to .NET System.Array.
        """
        return npext.ToSystemArray(arr)

    @staticmethod
    def asbool(arr):
        """
        Convert to bool array.
        """
        return npext.AsBoolArray(arr)

    @staticmethod
    def asint32(arr):
        """
        Convert to Int32 array.
        """
        return npext.AsInt32Array(arr)

    @staticmethod
    def asfloat(arr):
        """
        Convert to float array.
        """
        return npext.AsFloatArray(arr)

    @staticmethod
    def asdouble(arr):
        """
        Convert to double array.
        """
        return npext.AsDoubleArray(arr)

    @staticmethod
    def asdecimal(arr):
        """
        Convert to decimal array.
        """
        return npext.AsDecimalArray(arr)

    @staticmethod
    def ascomplex(arr):
        """
        Convert to complex array.
        """
        return npext.AsComplexArray(arr)

    @staticmethod
    def asbigint(arr):
        """
        Convert to BigInteger array.
        """
        return npext.AsBigIntArray(arr)

    @staticmethod
    def asobject(arr):
        """
        Convert to object array.
        """
        return npext.AsObjectArray(arr)

    @staticmethod
    def asstring(arr):
        """
        Convert to string array.
        """
        return npext.AsStringArray(arr)

    @staticmethod
    def asbyte(arr):
        """
        Convert to byte array.
        """
        return npext.AsByteArray(arr)

    @staticmethod
    def assbyte(arr):
        """
        Convert to sbyte array.
        """
        return npext.AsSByteArray(arr)

    @staticmethod
    def asint16(arr):
        """
        Convert to Int16 array.
        """
        return npext.AsInt16Array(arr)

    @staticmethod
    def asuint16(arr):
        """
        Convert to UInt16 array.
        """
        return npext.AsUInt16Array(arr)

    @staticmethod
    def asuint32(arr):
        """
        Convert to UInt32 array.
        """
        return npext.AsUInt32Array(arr)

    @staticmethod
    def asint64(arr):
        """
        Convert to Int64 array.
        """
        return npext.AsInt64Array(arr)

    @staticmethod
    def asuint64(arr):
        """
        Convert to UInt64 array.
        """
        return npext.AsUInt64Array(arr)

    @staticmethod
    def ptp(arr, axis=None, out=None, keepdims=False):
        """
        Range of values (maximum - minimum) along an axis.
        """
        return npext.ptp(arr, axis, out, keepdims)

    @staticmethod
    def amax(arr, axis=None, out=None, keepdims=False):
        """
        Return the maximum of an array or maximum along an axis.
        """
        return npext.AMax(arr, axis, out, keepdims)

    @staticmethod
    def amin(arr, axis=None, out=None, keepdims=False):
        """
        Return the minimum of an array or minimum along an axis.
        """
        return npext.AMin(arr, axis, out, keepdims)

    @staticmethod
    def prod(arr, axis=None, dtype=None, out=None):
        """
        Return the product of array elements over a given axis.
        """
        return npext.Prod(arr, axis, dtype, out)

    @staticmethod
    def cumprod(arr, axis=None, dtype=None, out=None):
        """
        Return the cumulative product of elements along a given axis.
        """
        return npext.CumProd(arr, axis, dtype, out)

    @staticmethod
    def partition(arr, kth, axis=None, kind='introselect', order=None):
        """
        Return a partitioned copy of an array.
        """
        return npext.partition(arr, kth, axis, kind, order)

    @staticmethod
    def to2darray(input_data, dtype=Double):
        """
        Convert 2D Python list/array to 2D array
        
        Parameters:
        input_data: 2D input array/list
        dtype: item type (System.Double by default)
        
        Returns:
        2D Array
        """
        if not input_data or not input_data[0]:
            return None
            
        rows = len(input_data)
        cols = len(input_data[0])
        
        array2d = Array.CreateInstance(dtype, rows, cols)
        
        for i in range(rows):
            for j in range(cols):
                array2d[i, j] = input_data[i][j]
                
        return array2d


namespace SciTrader.Data {
    public class DepthData {
        public double Price { get; private set; }
        public double Count { get; private set; }

        public DepthData(double price, double count) {
            Price = price;
            Count = count;
        }
    }
}

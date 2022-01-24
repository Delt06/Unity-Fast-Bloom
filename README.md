# Unity Fast Bloom URP

URP implementation of [Unity Fast Bloom](https://github.com/mightypanda/Unity-Fast-Bloom).

> Developed and with Unity 2020.3.16f1 LTS and URP package 10.5.1

## Benchmark

As a performance test, the implementation was compared to the bloom effect bundled with URP.

The results are average frame times measured in milliseconds.

| Device               | Base (No Effects)  | Fast Bloom | Fast Bloom + HDR | Built-in Bloom | Built-in Bloom + HDR |
|----------------------|--------------------|------------|------------------|----------------|----------------------|
| Xiaomi Mi 9T         | 20                 | 20         | 20               | 21             | 21                   |
| Xiaomi Redmi Note 8T | 21                 | 22         | 22.5             | 22             | 22.5                 |
| Honor 7C             | 25                 | 28         | 29               | 28             | 28                   | 
| Pocophone F1         | 24                 | 24         | 24               | 25             | 25.5                 |
| Huawei T3            | 25                 | 44.5       | 48               | 60.5           | 61                   | 


## References

https://catlikecoding.com/unity/tutorials/advanced-rendering/bloom

https://github.com/keijiro/KinoBloom

https://github.com/Unity-Technologies/PostProcessing

## License

The code is available under the [MIT license](LICENSE)

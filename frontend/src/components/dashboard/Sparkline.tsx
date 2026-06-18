import { ResponsiveContainer, AreaChart, Area } from "recharts";

interface SparklineProps {
  data: number[];
  color: string;
}

export function Sparkline({ data, color }: SparklineProps) {
  // Map raw number array to recharts object array format
  const chartData = (data && data.length > 0 ? data : [0, 0, 0, 0, 0]).map((val, i) => ({
    index: i,
    value: val,
  }));

  return (
    <div className="w-full h-14" style={{ contentVisibility: "auto" }}>
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={chartData} margin={{ top: 2, right: 2, left: 2, bottom: 2 }}>
          <defs>
            <linearGradient id={`gradient-${color.replace("#", "")}`} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={color} stopOpacity={0.4} />
              <stop offset="100%" stopColor={color} stopOpacity={0.0} />
            </linearGradient>
          </defs>
          <Area
            type="monotone"
            dataKey="value"
            stroke={color}
            strokeWidth={1.5}
            fill={`url(#gradient-${color.replace("#", "")})`}
            dot={false}
            isAnimationActive={false} // Disable animations for performance in fast scroll loops
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}
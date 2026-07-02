interface SparklineProps {
  data: number[];
  color: string;
}

export function Sparkline({ data, color }: SparklineProps) {
  const chartData = (data && data.length > 0 ? data : [0, 0, 0, 0, 0]);
  const width = 100;
  const height = 56;
  const padding = 2;

  const min = Math.min(...chartData);
  const max = Math.max(...chartData);
  const range = max - min || 1;

  const points = chartData.map((value, index) => {
    const x = padding + (index / (chartData.length - 1)) * (width - padding * 2);
    const y = height - padding - ((value - min) / range) * (height - padding * 2);
    return `${x},${y}`;
  }).join(' ');

  const gradientId = `gradient-${color.replace('#', '')}`;

  return (
    <svg width="100%" height="100%" viewBox={`0 0 ${width} ${height}`} preserveAspectRatio="none">
      <defs>
        <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity="0.4" />
          <stop offset="100%" stopColor={color} stopOpacity="0.0" />
        </linearGradient>
      </defs>
      <path
        d={`M ${points} L ${width - padding},${height - padding} L ${padding},${height - padding} Z`}
        fill={`url(#${gradientId})`}
        stroke="none"
      />
      <path
        d={`M ${points}`}
        fill="none"
        stroke={color}
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

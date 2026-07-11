import { useEffect, useRef, useState } from "react";

interface PriceDataPoint {
  Id?: number;
  id?: number;
  ListingId?: string;
  listingId?: string;
  Price?: number;
  price?: number;
  CurrencyCode?: string;
  currencyCode?: string;
  PriceInUsd?: number | null;
  priceInUsd?: number | null;
  RecordedAt?: string;
  recordedAt?: string;
  ScrapedAt?: string;
  scrapedAt?: string;
}

interface PriceHistoryChartProps {
  dataPoints: PriceDataPoint[];
  currencyCode: string;
}

export function PriceHistoryChart({ dataPoints, currencyCode }: PriceHistoryChartProps) {
  const pathRef = useRef<SVGPathElement>(null);
  const [pathLength, setPathLength] = useState(0);
  const normalizedPoints = dataPoints
    .map((point) => ({
      price: point.Price ?? point.price ?? 0,
      recordedAt: point.RecordedAt ?? point.recordedAt ?? new Date().toISOString(),
    }))
    .filter((point) => point.price > 0);

  // Recalculate SVG path length on data updates to drive the stroke-dashoffset animation
  useEffect(() => {
    if (pathRef.current) {
      try {
        const length = pathRef.current.getTotalLength();
        setPathLength(length);
      } catch {
        // Fallback for environments where path length calculation fails on first paint
        setPathLength(2000);
      }
    }
  }, [dataPoints]);

  if (normalizedPoints.length === 0) {
    return (
      <div className="flex h-72 items-center justify-center rounded-2xl border border-border-custom bg-surface/50 text-text-secondary">
        <div className="text-center">
          <p className="text-sm">No price history records found.</p>
          <p className="text-xs text-text-muted mt-1">Add alert tracking configuration or run the scraper.</p>
        </div>
      </div>
    );
  }

  const svgWidth = 800;
  const svgHeight = 350;
  const xPadding = 65;
  const yPadding = 45;

  const prices = normalizedPoints.map((dp) => dp.price);
  const minPrice = Math.min(...prices);
  const maxPrice = Math.max(...prices);
  const priceRange = maxPrice - minPrice || 1;

  const chartWidth = svgWidth - xPadding - 30; // some right margin
  const chartHeight = svgHeight - yPadding * 2;

  // Map data to SVG grid coordinate values
  const coords = normalizedPoints.map((dp, i) => {
    const x = xPadding + (i / (normalizedPoints.length - 1 || 1)) * chartWidth;
    const y = svgHeight - yPadding - ((dp.price - minPrice) / priceRange) * chartHeight;
    return {
      x,
      y,
      price: dp.price,
      date: new Date(dp.recordedAt).toLocaleDateString(undefined, {
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      }),
    };
  });

  // Construct SVG Path string
  let linePath = "";
  coords.forEach((coord, i) => {
    if (i === 0) linePath += `M ${coord.x} ${coord.y}`;
    else linePath += ` L ${coord.x} ${coord.y}`;
  });

  // Fill gradient area below price line
  let areaPath = "";
  if (coords.length > 0) {
    areaPath = `${linePath} L ${coords[coords.length - 1].x} ${svgHeight - yPadding} L ${coords[0].x} ${svgHeight - yPadding} Z`;
  }

  // Grid line levels
  const yGridLinesCount = 5;
  const yGridLines = Array.from({ length: yGridLinesCount }).map((_, idx) => {
    const val = minPrice + (priceRange / (yGridLinesCount - 1)) * idx;
    const y = svgHeight - yPadding - (idx / (yGridLinesCount - 1)) * chartHeight;
    return { y, value: val };
  });

  // Get date labels for horizontal axes (start, middle, end)
  const xLabels: { x: number; text: string }[] = [];
  if (coords.length > 0) {
    xLabels.push({ x: coords[0].x, text: coords[0].date.split(",")[0] });
    if (coords.length > 2) {
      const mid = Math.floor(coords.length / 2);
      xLabels.push({ x: coords[mid].x, text: coords[mid].date.split(",")[0] });
    }
    if (coords.length > 1) {
      const last = coords[coords.length - 1];
      xLabels.push({ x: last.x, text: last.date.split(",")[0] });
    }
  }

  return (
    <div className="w-full relative">
      <svg
        viewBox={`0 0 ${svgWidth} ${svgHeight}`}
        className="w-full h-auto select-none overflow-visible"
      >
          <defs>
            <linearGradient id="svgChartAreaGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#6c63ff" stopOpacity="0.18" />
              <stop offset="100%" stopColor="#6c63ff" stopOpacity="0.0" />
            </linearGradient>
            <linearGradient id="lineGlow" x1="0" y1="0" x2="1" y2="0">
              <stop offset="0%" stopColor="#6c63ff" />
              <stop offset="50%" stopColor="#00d4ff" />
              <stop offset="100%" stopColor="#6c63ff" />
            </linearGradient>
          </defs>

          {/* Grid lines & levels */}
          {yGridLines.map((gl, i) => (
            <g key={i} className="opacity-75">
              <line
                x1={xPadding}
                y1={gl.y}
                x2={svgWidth - 20}
                y2={gl.y}
                stroke="rgba(108, 99, 255, 0.1)"
                strokeWidth="1"
              />
              <text
                x={xPadding - 12}
                y={gl.y + 4}
                textAnchor="end"
                className="fill-text-secondary font-mono text-[11px] font-medium"
              >
                {currencyCode} {gl.value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </text>
            </g>
          ))}

          {/* X Axis labels */}
          {xLabels.map((xl, i) => (
            <text
              key={i}
              x={xl.x}
              y={svgHeight - yPadding + 20}
              textAnchor="middle"
              className="fill-text-secondary font-sans text-[11px] font-medium"
            >
              {xl.text}
            </text>
          ))}

          {/* Area under the line */}
          {areaPath && (
            <path
              d={areaPath}
              fill="url(#svgChartAreaGradient)"
            />
          )}

          {/* Animated line path */}
          {linePath && (
            <path
              ref={pathRef}
              d={linePath}
              fill="none"
              stroke="url(#lineGlow)"
              strokeWidth="3"
              strokeLinecap="round"
              strokeLinejoin="round"
              style={{
                strokeDasharray: pathLength || 2000,
                strokeDashoffset: pathLength || 2000,
                animation: "drawSvgPath 2.5s cubic-bezier(0.22, 1, 0.36, 1) forwards",
              }}
            />
          )}

        </svg>

      <style>{`
        @keyframes drawSvgPath {
          to {
            stroke-dashoffset: 0;
          }
        }
      `}</style>
    </div>
  );
}

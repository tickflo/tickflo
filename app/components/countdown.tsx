import { useEffect, useState } from 'react';

interface CountdownProps {
  createdAt: Date;
  maxAge: number; // Max age in seconds
}

const Countdown = ({ createdAt, maxAge }: CountdownProps) => {
  const [timeLeft, setTimeLeft] = useState(() => {
    const expiresAt = Number(createdAt) + maxAge * 1000;
    return Math.max(0, expiresAt - Date.now());
  });

  useEffect(() => {
    if (timeLeft <= 0) return;

    const interval = setInterval(() => {
      const remaining = Math.max(
        0,
        Number(createdAt) + maxAge * 1000 - Date.now(),
      );
      setTimeLeft(remaining);
      if (remaining <= 0) clearInterval(interval);
    }, 1000);

    return () => clearInterval(interval);
  }, [createdAt, maxAge, timeLeft]);

  const minutes = Math.floor(timeLeft / 60000);
  const seconds = Math.floor((timeLeft % 60000) / 1000);

  if (minutes > 0 && !seconds) {
    return (
      <span>
        {`${minutes} minutes and ${seconds.toString().padStart(2, '0')}`}{' '}
        seconds
      </span>
    );
  }

  if (minutes > 0 && seconds > 0) {
    return (
      <span>
        {`${minutes} minutes and ${seconds.toString().padStart(2, '0')}`}{' '}
        seconds
      </span>
    );
  }

  if (!minutes && seconds > 0) {
    return <span>{`${seconds.toString().padStart(2, '0')}`} seconds</span>;
  }

  return '';
};

export default Countdown;

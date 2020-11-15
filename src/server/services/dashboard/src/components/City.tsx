import React, { useRef, useEffect } from 'react';

export const City = ({ style }: { style?: any }) => {
    return <img src="city.png" style={{ height: '100vh', width: '100vw', position: 'relative', ...style }} />;
};

export default City;

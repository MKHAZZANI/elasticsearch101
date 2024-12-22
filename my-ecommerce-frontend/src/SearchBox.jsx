import React, { useState, useEffect } from 'react';
import axios from 'axios';

function ProductSearchAndDisplay() {
  const [allProducts, setAllProducts] = useState([]);  
  const [displayedProducts, setDisplayedProducts] = useState([]);
  const [query, setQuery] = useState('');

  useEffect(() => {
    
    fetchAllProducts();
  }, []);

  async function fetchAllProducts() {
    try {
      const response = await axios.get('http://localhost:5257/api/Products');
      setAllProducts(response.data);
      setDisplayedProducts(response.data); 
    } catch (error) {
      console.error(error);
    }
  }

    useEffect(() => {
    const timerId = setTimeout(() => {
      handleSearch(query);
    }, 300);
    return () => clearTimeout(timerId);
  }, [query]);

  async function handleSearch(input) {
    if (!input) {
      
      setDisplayedProducts(allProducts);
      return;
    }

    try {
      const response = await axios.get('http://localhost:5257/api/Products/autocomplete', {
        params: { query: input }
      });
      setDisplayedProducts(response.data); 
    } catch (error) {
      console.error(error);
    }
  }

  return (
    <div style={{ margin: '50px' }}>
      <input
        style={{ width: '300px', padding: '8px' }}
        type="text"
        placeholder="Type to search..."
        value={query}
        onChange={(e) => setQuery(e.target.value)}
      />

     
      <div style={{ marginTop: '20px' }}>
        {displayedProducts.map((product) => (
          <div 
            key={product.id} 
            style={{
              border: '1px solid #ccc',
              padding: '8px',
              marginBottom: '10px',
              display: 'flex',
              gap: '1rem'
            }}
          >
            
            {product.imageUrl && (
              <img
                src={product.imageUrl}
                alt="Product"
                style={{ width: '100px', height: 'auto' }}
              />
            )}
            <div>
              <h2 style={{ margin: '0 0 5px' }}>{product.title}</h2>
              <p style={{ margin: 0 }}>{product.description}</p>
              <p style={{ margin: 0, fontWeight: 'bold' }}>
                ${product.price}
              </p>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

export default ProductSearchAndDisplay;

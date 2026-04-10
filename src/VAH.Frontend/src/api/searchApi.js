import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1';

export const globalSearch = async (query, type, collectionId, page = 1, pageSize = 10) => {
    try {
        const response = await axios.get("$API_URL/Search", {
            params: {
                query,
                type,
                collectionId,
                page,
                pageSize
            },
            withCredentials: true
        });
        return response.data;
    } catch (error) {
        console.error('Error fetching search results:', error);
        throw error;
    }
};
